#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace PrettyGenocide.Editor
{
    public static class PGPrototypeSceneBuilder
    {
        private const string Root = "Assets/PrettyGenocide/Generated";

        [MenuItem("Tools/Pretty Genocide/Open Prototype Scene")]
        public static void OpenPrototype()
        {
            if (EditorApplication.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(Root + "/PrettyGenocide_Prototype.unity");
        }

        [MenuItem("Tools/Pretty Genocide/Create Prototype Scene")]
        public static void CreateScene()
        {
#if !ENABLE_INPUT_SYSTEM
            EditorUtility.DisplayDialog("Input System required",
                "Install Input System in Package Manager, then set Active Input Handling to Input System Package (New) or Both and restart the editor.", "OK");
            return;
#else
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Stop Play Mode", "Create the scene outside Play Mode.", "OK");
                return;
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            int playerLayer;
            int targetLayer;
            int bulletLayer;
            int enemyBulletLayer;
            try
            {
                playerLayer = EnsureLayer("PG_Player");
                targetLayer = EnsureLayer("PG_Target");
                bulletLayer = EnsureLayer("PG_Bullet");
                enemyBulletLayer = EnsureLayer("PG_EnemyBullet");
            }
            catch (Exception e)
            {
                if (Application.isBatchMode) throw;
                EditorUtility.DisplayDialog("Layer setup failed", e.Message, "OK");
                return;
            }
            EnsureFolder(Root);
            string runFolder = AssetDatabase.GenerateUniqueAssetPath(Root + "/PrototypeAssets");
            EnsureFolder(runFolder);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.48f, 0.47f, 0.57f);
            RenderSettings.fog = false;
            RenderSettings.skybox = null;

            Material floor = MakeMaterial(runFolder, "Floor", new Color(0.19f, 0.20f, 0.27f));
            Material wall = MakeMaterial(runFolder, "Walls", new Color(0.35f, 0.32f, 0.44f));
            Material trim = MakeMaterial(runFolder, "ArenaTrim", new Color(0.61f, 0.32f, 0.49f));
            Material pink = MakeMaterial(runFolder, "GirlPink", new Color(0.98f, 0.33f, 0.67f));
            Material pale = MakeMaterial(runFolder, "GirlWhite", new Color(0.95f, 0.90f, 0.94f));
            Material skin = MakeMaterial(runFolder, "GirlSkin", new Color(0.98f, 0.79f, 0.70f));
            Material hair = MakeMaterial(runFolder, "GirlHair", new Color(0.26f, 0.16f, 0.34f));
            Material dark = MakeMaterial(runFolder, "RevolverDark", new Color(0.16f, 0.14f, 0.20f));
            Material gold = MakeMaterial(runFolder, "BulletGold", new Color(1f, 0.73f, 0.22f), true);
            Material returning = MakeMaterial(runFolder, "BulletReturnPink", new Color(1f, 0.14f, 0.60f), true);
            Material enemyMat = MakeMaterial(runFolder, "Enemy", new Color(0.31f, 0.69f, 0.70f));
            Material enemyShotMat = MakeMaterial(runFolder, "EnemyShot", new Color(1f, 0.30f, 0.14f), true);
            Material trail = MakeMaterial(runFolder, "Trail", new Color(1f, 0.67f, 0.75f), true);

            Transform arena = new GameObject("TrainingArena").transform;
            Box("Floor", arena, new Vector3(0f, -0.25f, 9f), new Vector3(32f, 0.5f, 44f), floor, true, 0);
            Box("Backstop", arena, new Vector3(0f, 1.5f, 31f), new Vector3(32f, 3f, 0.5f), wall, true, 0);
            Box("BackWall", arena, new Vector3(0f, 1.5f, -13f), new Vector3(32f, 3f, 0.5f), wall, true, 0);
            Box("LeftWall", arena, new Vector3(-16f, 1.5f, 9f), new Vector3(0.5f, 3f, 44f), wall, true, 0);
            Box("RightWall", arena, new Vector3(16f, 1.5f, 9f), new Vector3(0.5f, 3f, 44f), wall, true, 0);
            Box("HighCover", arena, new Vector3(6f, 1f, 7f), new Vector3(3f, 2f, 3f), wall, true, 0);
            Box("LowCover", arena, new Vector3(-6f, 0.55f, 13f), new Vector3(4f, 1.1f, 3f), wall, true, 0);
            // This bridge checks whether the collider stays crouched until there is headroom.
            Box("SlideTunnel", arena, new Vector3(-8f, 1.5f, 2f), new Vector3(4f, 0.7f, 3f), wall, true, 0);
            for (int z = -8; z <= 28; z += 4)
                Box("FloorStripe", arena, new Vector3(0f, 0.005f, z), new Vector3(0.10f, 0.01f, 1.2f), trim, false, 2);

            GameObject lightObject = new GameObject("KeyLight");
            Light key = lightObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.4f;
            key.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            GameObject player = new GameObject("Player");
            player.layer = playerLayer;
            player.transform.position = new Vector3(0f, 0.02f, -4f);
            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.30f;
            cc.center = new Vector3(0f, 0.90f, 0f);
            cc.stepOffset = 0.22f;
            cc.skinWidth = 0.03f;
            cc.minMoveDistance = 0f;
            cc.slopeLimit = 50f;
            PGInput input = player.AddComponent<PGInput>();
            input.freeCursorAim = true;
            PGPlayerMotor motor = player.AddComponent<PGPlayerMotor>();
            PGRevolver revolver = player.AddComponent<PGRevolver>();
            Transform pose = Child("VisualPose_REPLACE_WITH_VOXEL_MODEL", player.transform, Vector3.zero);
            AddGirl(pose, playerLayer, pink, pale, skin, hair);
            Transform gunPivot = Child("WeaponPivot", pose, new Vector3(0.45f, 1.20f, 0.12f));
            Box("RevolverBody", gunPivot, new Vector3(0f, 0f, 0.20f), new Vector3(0.16f, 0.18f, 0.44f), dark, false, playerLayer);
            Box("RevolverBarrel", gunPivot, new Vector3(0f, 0.035f, 0.48f), new Vector3(0.10f, 0.10f, 0.27f), gold, false, playerLayer);
            Box("RevolverGrip", gunPivot, new Vector3(0f, -0.14f, 0.03f), new Vector3(0.13f, 0.26f, 0.14f), dark, false, playerLayer);
            Transform muzzle = Child("Muzzle", gunPivot, new Vector3(0f, 0.035f, 0.65f));
            Transform catchSocket = Child("RecallSocket", pose, new Vector3(0f, 0.98f, 0.20f));
            Light flash = Child("MuzzleFlash", muzzle, Vector3.zero).gameObject.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.color = new Color(1f, 0.5f, 0.7f);
            flash.intensity = 2f;
            flash.range = 3f;
            flash.enabled = false;

            GameObject rigObject = new GameObject("CameraRig");
            PGCameraRig rig = rigObject.AddComponent<PGCameraRig>();
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(rigObject.transform);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 180f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.10f, 0.09f, 0.16f);
            camera.fieldOfView = 72f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0.65f, 2.8f, -9.5f);
            cameraObject.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
            rig.target = player.transform;
            rig.input = input;
            rig.viewCamera = camera;
            rig.aimMask = (1 << 0) | (1 << targetLayer);
            rig.obstructionMask = 1 << 0;
            motor.input = input;
            motor.cameraRig = rig;
            motor.visualPose = pose;
            motor.standingMask = (1 << 0) | (1 << targetLayer);
            revolver.input = input;
            revolver.cameraRig = rig;
            revolver.weaponPivot = gunPivot;
            revolver.muzzle = muzzle;
            revolver.recallSocket = catchSocket;
            revolver.muzzleFlash = flash;
            revolver.outboundMaterial = gold;
            revolver.returnMaterial = returning;
            revolver.trailMaterial = trail;
            revolver.outboundMask = (1 << 0) | (1 << targetLayer);
            revolver.worldMask = 1 << 0;
            revolver.returnDamageMask = (1 << targetLayer) | (1 << enemyBulletLayer);
            revolver.projectileLayer = bulletLayer;

            List<PGDamageable> targets = new List<PGDamageable>();
            targets.Add(AddTarget("Target_A", new Vector3(-3f, 0f, 7f), targetLayer, enemyMat, pale));
            targets.Add(AddTarget("Target_B", new Vector3(0f, 0f, 11f), targetLayer, enemyMat, pale));
            targets.Add(AddTarget("Target_C", new Vector3(0f, 0f, 16f), targetLayer, enemyMat, pale));
            targets.Add(AddTarget("Target_D", new Vector3(0f, 0f, 22f), targetLayer, enemyMat, pale));
            targets.Add(AddTarget("Turret_Left", new Vector3(-10f, 0f, 22f), targetLayer, trim, pale));
            targets.Add(AddTarget("Turret_Right", new Vector3(10f, 0f, 23f), targetLayer, trim, pale));
            List<PGTrainingTurret> turrets = new List<PGTrainingTurret>();
            for (int i = targets.Count - 2; i < targets.Count; i++)
            {
                PGTrainingTurret turret = targets[i].gameObject.AddComponent<PGTrainingTurret>();
                turret.player = player.transform;
                turret.health = targets[i];
                turret.muzzle = Child("EnemyMuzzle", targets[i].transform, new Vector3(0f, 1.25f, -0.6f));
                turret.projectileMaterial = enemyShotMat;
                turret.projectileLayer = enemyBulletLayer;
                turret.projectileHitMask = (1 << 0) | (1 << playerLayer);
                turret.attacksEnabled = false;
                turrets.Add(turret);
            }
            PGPrototypeHUD hud = new GameObject("PrototypeHUD").AddComponent<PGPrototypeHUD>();
            hud.input = input;
            hud.player = motor;
            hud.revolver = revolver;
            hud.cameraRig = rig;
            hud.turrets = turrets.ToArray();
            Physics.SyncTransforms();
            AssetDatabase.SaveAssets();
            string scenePath = AssetDatabase.GenerateUniqueAssetPath(Root + "/PrettyGenocide_Prototype.unity");
            EditorSceneManager.SaveScene(scene, scenePath);
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            Selection.activeGameObject = player;
            Debug.Log("Pretty Genocide prototype scene created: " + scenePath + "\nPress Play. F2 toggles turret fire. No existing scene or material asset was overwritten.");
#endif
        }

        // Unity -batchmode -executeMethod PrettyGenocide.Editor.PGPrototypeSceneBuilder.PrepareAndValidate
        public static void PrepareAndValidate()
        {
            const string path = Root + "/PrettyGenocide_Prototype.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) CreateScene();
            PGPrototypeSelfTests.Run();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                throw new InvalidOperationException("Prototype scene was not created.");
            Debug.Log("PG_SETUP_PASS: scene saved and ammo ledger validated.");
        }

        private static PGDamageable AddTarget(string name, Vector3 position, int layer, Material body, Material face)
        {
            GameObject root = new GameObject(name);
            root.layer = layer;
            root.transform.position = position;
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.98f, 0f);
            collider.size = new Vector3(0.95f, 1.96f, 0.75f);
            Box("Body", root.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.95f, 1.35f, 0.75f), body, false, layer);
            Box("Head", root.transform, new Vector3(0f, 1.7f, 0f), new Vector3(0.7f, 0.45f, 0.65f), body, false, layer);
            Box("Face", root.transform, new Vector3(0f, 1.72f, -0.34f), new Vector3(0.43f, 0.11f, 0.03f), face, false, layer);
            PGDamageable damageable = root.AddComponent<PGDamageable>();
            damageable.displayName = name.StartsWith("Turret") ? "훈련 포탑" : "훈련 표적";
            return damageable;
        }

        private static void AddGirl(Transform pose, int layer, Material pink, Material pale, Material skin, Material hair)
        {
            Box("Boot_L", pose, new Vector3(-0.16f, 0.17f, 0f), new Vector3(0.23f, 0.34f, 0.30f), pink, false, layer);
            Box("Boot_R", pose, new Vector3(0.16f, 0.17f, 0f), new Vector3(0.23f, 0.34f, 0.30f), pink, false, layer);
            Box("Leg_L", pose, new Vector3(-0.16f, 0.47f, 0f), new Vector3(0.19f, 0.28f, 0.21f), pale, false, layer);
            Box("Leg_R", pose, new Vector3(0.16f, 0.47f, 0f), new Vector3(0.19f, 0.28f, 0.21f), pale, false, layer);
            Box("SkirtHem", pose, new Vector3(0f, 0.68f, 0f), new Vector3(0.73f, 0.17f, 0.55f), pale, false, layer);
            Box("Skirt", pose, new Vector3(0f, 0.82f, 0f), new Vector3(0.62f, 0.18f, 0.47f), pink, false, layer);
            Box("Torso", pose, new Vector3(0f, 1.08f, 0f), new Vector3(0.43f, 0.37f, 0.30f), pink, false, layer);
            Box("Arm_L", pose, new Vector3(-0.32f, 1.04f, 0.03f), new Vector3(0.17f, 0.40f, 0.19f), pale, false, layer);
            Box("Arm_R", pose, new Vector3(0.32f, 1.12f, 0.09f), new Vector3(0.17f, 0.31f, 0.19f), pale, false, layer);
            Box("Head", pose, new Vector3(0f, 1.50f, 0f), new Vector3(0.47f, 0.44f, 0.43f), skin, false, layer);
            Box("HairTop", pose, new Vector3(0f, 1.74f, -0.015f), new Vector3(0.55f, 0.15f, 0.51f), hair, false, layer);
            Box("HairBack", pose, new Vector3(0f, 1.48f, -0.24f), new Vector3(0.52f, 0.43f, 0.14f), hair, false, layer);
            Box("Tail_L", pose, new Vector3(-0.37f, 1.44f, -0.09f), new Vector3(0.20f, 0.43f, 0.23f), hair, false, layer);
            Box("Tail_R", pose, new Vector3(0.37f, 1.44f, -0.09f), new Vector3(0.20f, 0.43f, 0.23f), hair, false, layer);
            Box("Ribbon_L", pose, new Vector3(-0.32f, 1.68f, 0f), new Vector3(0.24f, 0.14f, 0.13f), pink, false, layer);
            Box("Ribbon_R", pose, new Vector3(0.32f, 1.68f, 0f), new Vector3(0.24f, 0.14f, 0.13f), pink, false, layer);
        }

        private static Transform Child(string name, Transform parent, Vector3 localPosition)
        {
            GameObject go = new GameObject(name);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        private static GameObject Box(string name, Transform parent, Vector3 localPosition, Vector3 scale,
            Material material, bool solid, int layer)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.layer = layer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static Material MakeMaterial(string folder, string name, Color color, bool unlit = false)
        {
            bool srp = GraphicsSettings.currentRenderPipeline != null;
            Shader shader = Shader.Find(srp
                ? (unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")
                : (unlit ? "Unlit/Color" : "Standard"));
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) throw new InvalidOperationException("No supported shader found. Use a URP or Built-in project, not HDRP.");
            Material material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.25f);
            AssetDatabase.CreateAsset(material, folder + "/" + name + ".mat");
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            string parent = path.Substring(0, split);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
        }

        private static int EnsureLayer(string name)
        {
            int existing = LayerMask.NameToLayer(name);
            if (existing >= 0) return existing;
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets.Length == 0) throw new InvalidOperationException("Could not read TagManager.asset.");
            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 8; i < 32; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(layer.stringValue)) continue;
                layer.stringValue = name;
                tagManager.ApplyModifiedProperties();
                return i;
            }
            throw new InvalidOperationException("Four free user layers are required: PG_Player, PG_Target, PG_Bullet, PG_EnemyBullet. Existing layers will not be overwritten.");
        }
    }
}
#endif
