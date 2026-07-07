using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;

public class AnimalPrefabSetup : Editor
{
    [MenuItem("TheMadang/Create Animal Prefabs")]
    public static void CreateAnimalPrefabs()
    {
        string prefabFolder = "Assets/Prefabs/Animals";
        Directory.CreateDirectory(Application.dataPath + "/Prefabs/Animals");
        AssetDatabase.Refresh();

        CreateCatPrefabs(prefabFolder);
        CreateDogPrefabs(prefabFolder);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료!", "동물 프리팹 생성 완료!\n Assets/Prefabs/Animals 폴더를 확인하세요.", "확인");
    }

    static void CreateCatPrefabs(string prefabFolder)
    {
        string[] catTypes = { "Cat-1", "Cat-2", "Cat-3", "Cat-4", "Cat-5", "Cat-6" };
        AnimalType[] animalTypes = {
            AnimalType.Cat, AnimalType.Cat, AnimalType.Cat,
            AnimalType.Cat, AnimalType.Cat, AnimalType.Cat
        };
        string[] catNames = { "나비", "야옹이", "치즈", "까망이", "흰둥이", "호랑이" };

        for (int i = 0; i < catTypes.Length; i++)
        {
            string spritesPath = $"Assets/Pet Cats pack/Sprites/{catTypes[i]}";
            if (!AssetDatabase.IsValidFolder(spritesPath)) continue;

            var animator = CreateAnimatorController(catTypes[i], spritesPath, "Cat");
            if (animator == null) continue;

            CreateAnimalPrefab(prefabFolder, catTypes[i], catNames[i], animalTypes[i], animator, spritesPath, $"{catTypes[i]}-Idle");
        }
    }

    static void CreateDogPrefabs(string prefabFolder)
    {
        var dogs = new[]
        {
            ("Dog-1-Golden-Retriever", "Golden-Retriever", "골든이"),
            ("Dog-2-Akita",           "Akita",            "아키타"),
            ("Dog-3-Great-Dane",      "Great-Dane",       "그레이트"),
            ("Dog-4-Schnauzer",       "Schnauzer",        "슈나우저"),
            ("Dog-5-Saint-Bernard",   "Saint-Bernard",    "버나드"),
            ("Dog-6-Siberian-Husky",  "Siberian-Husky",  "허스키"),
        };

        foreach (var (folder, prefix, dogName) in dogs)
        {
            string spritesPath = $"Assets/Pet Dogs Pack/Sprites/{folder}";
            if (!AssetDatabase.IsValidFolder(spritesPath)) continue;

            var animator = CreateDogAnimatorController(folder, prefix, spritesPath);
            if (animator == null) continue;

            CreateAnimalPrefab(prefabFolder, folder, dogName, AnimalType.Dog, animator, spritesPath, $"{prefix}-idle");
        }
    }

    static AnimatorController CreateDogAnimatorController(string animalId, string prefix, string spritesPath)
    {
        string controllerPath = $"Assets/Prefabs/Animals/{animalId}_Animator.controller";
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var rootStateMachine = controller.layers[0].stateMachine;

        var animMap = new[] {
            ("idle", "Idle"), ("walk", "Walk"), ("run", "Run"),
            ("sitting", "Sitting"), ("sleeping", "Sleeping")
        };

        AnimatorState idleState = null;
        foreach (var (suffix, stateName) in animMap)
        {
            string[] guids = AssetDatabase.FindAssets($"{prefix}-{suffix}", new[] { spritesPath });
            if (guids.Length == 0) continue;

            string clipPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var clips = System.Linq.Enumerable.OfType<AnimationClip>(AssetDatabase.LoadAllAssetsAtPath(clipPath)).ToArray();
            if (clips.Length == 0) continue;

            var state = rootStateMachine.AddState(stateName);
            state.motion = clips[0];
            if (stateName == "Idle") idleState = state;
        }

        controller.AddParameter("isWalking", AnimatorControllerParameterType.Bool);
        if (idleState != null) rootStateMachine.defaultState = idleState;

        EditorUtility.SetDirty(controller);
        return controller;
    }

    static AnimatorController CreateAnimatorController(string animalId, string spritesPath, string animalType)
    {
        string controllerPath = $"Assets/Prefabs/Animals/{animalId}_Animator.controller";
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var rootStateMachine = controller.layers[0].stateMachine;

        // 애니메이션 클립 찾기
        string[] animNames = { "Idle", "Walk", "Run", "Sitting", "Sleeping1" };
        AnimatorState idleState = null;

        foreach (var animName in animNames)
        {
            string[] guids = AssetDatabase.FindAssets($"{animalId}-{animName}", new[] { spritesPath });
            if (guids.Length == 0) continue;

            string clipPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var clips = AssetDatabase.LoadAllAssetsAtPath(clipPath).OfType<AnimationClip>().ToArray();
            if (clips.Length == 0) continue;

            var clip = clips[0];
            var state = rootStateMachine.AddState(animName);
            state.motion = clip;

            if (animName == "Idle" || animName == "Sitting")
            {
                if (idleState == null) idleState = state;
            }
        }

        // 파라미터 추가
        controller.AddParameter("isWalking", AnimatorControllerParameterType.Bool);

        if (idleState != null)
            rootStateMachine.defaultState = idleState;

        EditorUtility.SetDirty(controller);
        return controller;
    }

    static void CreateAnimalPrefab(string prefabFolder, string animalId, string animalName,
        AnimalType animalType, AnimatorController animator, string spritesPath, string idleSearchTerm)
    {
        // 첫 번째 Idle 스프라이트 찾기 (idleSearchTerm = 전체 검색어)
        string[] guids = AssetDatabase.FindAssets(idleSearchTerm, new[] { spritesPath });
        Sprite firstSprite = null;
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            if (sprites.Length > 0) firstSprite = sprites[0];
        }

        // 프리팹 생성
        var go = new GameObject(animalId);
        var sr = go.AddComponent<SpriteRenderer>();
        if (firstSprite != null) sr.sprite = firstSprite;
        sr.sortingOrder = 1;

        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = animator;

        var animal = go.AddComponent<Animal>();
        animal.animalType = animalType;
        animal.animalName = animalName;

        // Collider 추가 (클릭 감지용)
        var collider = go.AddComponent<PolygonCollider2D>();

        string prefabPath = $"{prefabFolder}/{animalId}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        Debug.Log($"✅ {animalId} 프리팹 생성: {prefabPath}");
    }
}
