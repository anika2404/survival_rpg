using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Generates four-direction idle/walk animations and Animator Controllers for
/// the existing Scout and Fighter sprite sheets. Does not edit scenes or prefabs.
/// Controllers use MoveX, MoveY, and Speed, matching PlayerMovement.cs.
/// </summary>
public static class GenerateScoutFighterAnimations
{
    private const float FramesPerSecond = 8f;
    private static readonly string[] Directions = { "Down", "Up", "Left", "Right" };
    private static readonly Vector2[] DirectionVectors =
    {
        new Vector2(0f, -1f), // Down
        new Vector2(0f,  1f), // Up
        new Vector2(-1f, 0f), // Left
        new Vector2(1f,  0f)  // Right
    };

    [MenuItem("Tools/Character Animations/Generate Scout + Fighter")]
    public static void Generate()
    {
        const string scoutSheet = "Assets/art/Characters/scout_sheet.png";
        const string fighterSheet = "Assets/art/Characters/Fighter_sprites.png";
        const string outputRoot = "Assets/animations/Characters";

        if (AssetDatabase.LoadMainAssetAtPath(scoutSheet) == null ||
            AssetDatabase.LoadMainAssetAtPath(fighterSheet) == null)
        {
            EditorUtility.DisplayDialog(
                "Character animation setup",
                "Could not find both sprite sheets. Expected:\n" + scoutSheet + "\n" + fighterSheet +
                "\n\nCheck the file names and the case of the folders.",
                "OK");
            return;
        }

        EnsureFolder("Assets/animations");
        EnsureFolder(outputRoot);
        EnsureFolder(outputRoot + "/Scout");
        EnsureFolder(outputRoot + "/Fighter");

        bool outputsExist = HasOutputs(outputRoot + "/Scout", "Scout") ||
                            HasOutputs(outputRoot + "/Fighter", "Fighter");
        if (outputsExist && !EditorUtility.DisplayDialog(
                "Regenerate character animations?",
                "Animation files with the generated Scout/Fighter names already exist. Replace only those generated clips and controllers?",
                "Replace generated files", "Cancel"))
        {
            return;
        }

        try
        {
            CreateCharacter("Scout", scoutSheet, outputRoot + "/Scout");
            CreateCharacter("Fighter", fighterSheet, outputRoot + "/Fighter");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Animations generated",
                "Created Scout and Fighter directional Idle/Walk clips and Animator Controllers.\n\n" +
                "Output: " + outputRoot + "\n\n" +
                "Each controller has MoveX, MoveY, and Speed parameters. This tool did not edit any scenes or prefabs.",
                "Great");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Animation generation failed", ex.Message + "\n\nSee the Console for details.", "OK");
        }
    }

    private static void CreateCharacter(string characterName, string sheetPath, string outputFolder)
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath)
            .OfType<Sprite>()
            // The sprite names in these sheets aren't consistently row-major (Scout isn't).
            // Sort by actual rectangle position: top row to bottom row, left to right.
            .OrderByDescending(s => s.rect.y)
            .ThenBy(s => s.rect.x)
            .ToArray();

        if (sprites.Length < 12)
        {
            throw new InvalidOperationException(
                characterName + " sheet has only " + sprites.Length + " sliced sprites. " +
                "It needs 12 sprites (4 directions x 3 frames). In the sprite import settings, use Sprite Mode = Multiple and slice the sheet first.");
        }

        var clips = new Dictionary<string, AnimationClip>();
        for (int direction = 0; direction < 4; direction++)
        {
            Sprite[] rowFrames = sprites.Skip(direction * 3).Take(3).ToArray();
            if (rowFrames.Any(s => s == null))
                throw new InvalidOperationException("Missing sprites in " + characterName + " direction " + Directions[direction] + ".");

            string idleName = characterName + "_Idle_" + Directions[direction];
            string walkName = characterName + "_Walk_" + Directions[direction];
            clips[idleName] = CreateClip(outputFolder + "/" + idleName + ".anim", idleName, new[] { rowFrames[0] }, false);
            clips[walkName] = CreateClip(outputFolder + "/" + walkName + ".anim", walkName, rowFrames, true);
        }

        CreateController(outputFolder + "/" + characterName + ".controller", characterName, clips);
    }

    private static AnimationClip CreateClip(string assetPath, string clipName, Sprite[] frames, bool loop)
    {
        if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
            AssetDatabase.DeleteAsset(assetPath);

        var clip = new AnimationClip
        {
            name = clipName,
            frameRate = FramesPerSecond
        };

        // Add a final key at frame N so the last sprite receives a full frame of display time.
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i / FramesPerSecond,
                value = frames[i]
            };
        }
        keys[frames.Length] = new ObjectReferenceKeyframe
        {
            time = frames.Length / FramesPerSecond,
            value = frames[0]
        };

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, assetPath);
        return clip;
    }

    private static void CreateController(string assetPath, string characterName, Dictionary<string, AnimationClip> clips)
    {
        if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
            AssetDatabase.DeleteAsset(assetPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(assetPath);
        controller.name = characterName;
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState existingState in stateMachine.states)
            stateMachine.RemoveState(existingState.state);

        BlendTree idleTree = CreateDirectionalTree(
            controller, characterName + "_IdleDirectional", clips, characterName, "Idle");
        BlendTree walkTree = CreateDirectionalTree(
            controller, characterName + "_WalkDirectional", clips, characterName, "Walk");

        AnimatorState idleState = stateMachine.AddState("Idle");
        idleState.motion = idleTree;
        idleState.writeDefaultValues = true;
        AnimatorState walkState = stateMachine.AddState("Walk");
        walkState.motion = walkTree;
        walkState.writeDefaultValues = true;
        stateMachine.defaultState = idleState;

        AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
        toWalk.hasExitTime = false;
        toWalk.duration = 0.05f;
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.01f, "Speed");

        AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.05f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");

        EditorUtility.SetDirty(controller);
    }

    private static BlendTree CreateDirectionalTree(
        AnimatorController controller,
        string treeName,
        Dictionary<string, AnimationClip> clips,
        string characterName,
        string clipPrefix)
    {
        var tree = new BlendTree
        {
            name = treeName,
            blendType = BlendTreeType.SimpleDirectional2D,
            blendParameter = "MoveX",
            blendParameterY = "MoveY",
            useAutomaticThresholds = false,
            hideFlags = HideFlags.HideInHierarchy
        };

        for (int i = 0; i < Directions.Length; i++)
        {
            string clipName = characterName + "_" + clipPrefix + "_" + Directions[i];
            tree.AddChild(clips[clipName], DirectionVectors[i]);
        }
        AssetDatabase.AddObjectToAsset(tree, controller);
        return tree;
    }

    private static bool HasOutputs(string folder, string characterName)
    {
        if (AssetDatabase.LoadMainAssetAtPath(folder + "/" + characterName + ".controller") != null)
            return true;
        foreach (string direction in Directions)
        {
            if (AssetDatabase.LoadMainAssetAtPath(folder + "/" + characterName + "_Idle_" + direction + ".anim") != null ||
                AssetDatabase.LoadMainAssetAtPath(folder + "/" + characterName + "_Walk_" + direction + ".anim") != null)
                return true;
        }
        return false;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folderName = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
            throw new InvalidOperationException("Could not create folder: " + path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
