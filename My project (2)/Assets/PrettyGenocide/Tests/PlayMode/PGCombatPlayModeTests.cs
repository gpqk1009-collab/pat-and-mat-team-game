using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PrettyGenocide.Tests
{
    // These tests run the real scene, CharacterController, physics queries and Update loop.
    // Input snapshots are injected so OS focus and the user's mouse cannot affect assertions.
    public sealed class PGCombatPlayModeTests
    {
        private PGInput input;
        private PGRevolver weapon;
        private PGPlayerMotor motor;
        private PGCameraRig rig;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync("PrettyGenocide_Prototype", LoadSceneMode.Single);
            input = Object.FindFirstObjectByType<PGInput>();
            weapon = Object.FindFirstObjectByType<PGRevolver>();
            motor = Object.FindFirstObjectByType<PGPlayerMotor>();
            rig = Object.FindFirstObjectByType<PGCameraRig>();
            Assert.That(input, Is.Not.Null);
            input.enabled = false;
            input.freeCursorAim = false;
            SetInput("Active", true);
            SetInput("Pointer", new Vector2(Screen.width / 2f, Screen.height / 2f));
            weapon.aimWarmup = 0f;
            weapon.shotInterval = 0.01f;
            yield return null;
            yield return new WaitForSeconds(0.15f);
        }

        [TearDown]
        public void TearDown() => Time.captureDeltaTime = 0f;

        private void SetInput(string name, object value) => typeof(PGInput)
            .GetField("<" + name + ">k__BackingField", Private).SetValue(input, value);

        private static void Invoke(object target, string method, params object[] args) => target.GetType()
            .GetMethod(method, Private).Invoke(target, args);

        private IEnumerator Click(bool secondary = false)
        {
            string key = secondary ? "SecondaryPressed" : "PrimaryPressed";
            SetInput(key, true);
            yield return null;
            SetInput(key, false);
            yield return null;
        }

        private PGRecallBullet Bullet() => Object.FindObjectsByType<PGRecallBullet>(FindObjectsSortMode.None)
            .OrderBy(b => b.Id).Last();

        private IEnumerator UntilRecovered()
        {
            float deadline = Time.realtimeSinceStartup + 5f;
            while (weapon.IsRecalling && Time.realtimeSinceStartup < deadline)
            {
                Assert.That(weapon.Ammo + weapon.FieldBullets, Is.EqualTo(6));
                yield return null;
            }
            Assert.That(weapon.IsRecalling, Is.False, "Recall timed out");
            Assert.That(weapon.Ammo, Is.EqualTo(6));
        }

        [UnityTest]
        public IEnumerator FirstClickOnlyAims_ThenOneShot_ReleaseDoesNotRepeat()
        {
            yield return Click();
            Assert.That(weapon.IsAiming, Is.True);
            Assert.That(weapon.Ammo, Is.EqualTo(6));
            yield return Click();
            Assert.That(weapon.Ammo, Is.EqualTo(5));
            yield return new WaitForSeconds(0.15f);
            Assert.That(weapon.Ammo, Is.EqualTo(5));
            Assert.That(weapon.FieldBullets, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SixShots_AutoRecallKeepsAim_AndLocksFire()
        {
            weapon.autoRecallDelay = 0.5f;
            yield return Click();
            for (int i = 0; i < 6; i++) yield return Click();
            Assert.That(weapon.Ammo, Is.Zero);
            Assert.That(weapon.IsRecalling, Is.True);
            Assert.That(weapon.IsAiming, Is.True);
            yield return Click();
            Assert.That(weapon.FieldBullets, Is.EqualTo(6));
            yield return UntilRecovered();
            Assert.That(weapon.IsAiming, Is.True);
            yield return Click();
            Assert.That(weapon.Ammo, Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator ManualRecall_DeliversPhysically_CancelsAim_RejectsRepeatedClicks()
        {
            yield return Click();
            yield return Click();
            PGRecallBullet bullet = Bullet();
            bullet.transform.position = weapon.RecallPosition + Vector3.forward * 20f;
            weapon.returnSpeed = 2f;
            yield return Click(true);
            Assert.That(weapon.IsAiming, Is.False);
            Assert.That(weapon.Ammo, Is.EqualTo(5), "Requesting recall must not reload immediately");
            for (int i = 0; i < 3; i++) { yield return Click(true); yield return Click(); }
            Assert.That(weapon.IsAiming, Is.False);
            Assert.That(weapon.FieldBullets, Is.EqualTo(1));
            weapon.returnSpeed = 100f;
            yield return UntilRecovered();
            yield return Click();
            Assert.That(weapon.IsAiming, Is.True);
            Assert.That(weapon.Ammo, Is.EqualTo(6));
        }

        [UnityTest]
        public IEnumerator PartialArrival_DoesNotUnlockFiring()
        {
            weapon.returnSpeed = 1f;
            yield return Click();
            yield return Click();
            PGRecallBullet first = Bullet();
            yield return Click();
            PGRecallBullet second = Bullet();
            second.transform.position = weapon.RecallPosition + Vector3.forward * 20f;
            first.transform.position = weapon.RecallPosition;
            weapon.RecallNow();
            yield return null;
            yield return null;
            Assert.That(weapon.Ammo, Is.EqualTo(5));
            Assert.That(weapon.IsRecalling, Is.True);
            yield return Click();
            Assert.That(weapon.Ammo, Is.EqualTo(5));
            weapon.returnSpeed = 100f;
            yield return UntilRecovered();
        }

        private static PGDamageable Target(string name, Vector3 position, bool multiple = false)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.layer = LayerMask.NameToLayer("PG_Target");
            go.transform.position = position;
            if (multiple)
            {
                GameObject child = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                child.layer = go.layer;
                child.transform.SetParent(go.transform, false);
                child.transform.localScale = Vector3.one * 0.8f;
            }
            return go.AddComponent<PGDamageable>();
        }

        [UnityTest]
        public IEnumerator Return_PiercesWallAndMultipleTargets_DeduplicatesColliders_InterceptsProjectile()
        {
            yield return Click();
            yield return Click();
            PGRecallBullet bullet = Bullet();
            bullet.enabled = false;
            Vector3 hand = weapon.RecallPosition;
            PGDamageable a = Target("ReturnTest_A", hand + Vector3.forward * 3f, true);
            PGDamageable b = Target("ReturnTest_B", hand + Vector3.forward * 6f);
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = hand + Vector3.forward * 4.5f;
            wall.transform.localScale = new Vector3(2f, 2f, 0.4f);
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.layer = LayerMask.NameToLayer("PG_EnemyBullet");
            projectile.transform.position = hand + Vector3.forward * 7f;
            projectile.GetComponent<Collider>().isTrigger = true;
            PGEnemyProjectile enemyShot = projectile.AddComponent<PGEnemyProjectile>();
            enemyShot.Initialize(Vector3.zero, 0, 0);
            bullet.transform.position = hand + Vector3.forward * 9f;
            weapon.RecallNow();
            Invoke(bullet, "StepReturning", 0.12f);
            Invoke(bullet, "StepReturning", 0.12f);
            Assert.That(a.ReturnHits, Is.EqualTo(1));
            Assert.That(b.ReturnHits, Is.EqualTo(1));
            Assert.That(a.Health, Is.EqualTo(a.maxHealth - weapon.returnDamage));
            Assert.That(weapon.Ammo, Is.EqualTo(6));
            yield return null;
            Assert.That(enemyShot == null, Is.True);
        }

        [UnityTest]
        public IEnumerator Return_StartingInsideTarget_HitsOnlyOnceAcrossFrames()
        {
            yield return Click();
            yield return Click();
            PGRecallBullet bullet = Bullet();
            bullet.enabled = false;
            Vector3 p = weapon.RecallPosition + Vector3.forward * 5f;
            PGDamageable target = Target("InitialOverlap", p, true);
            bullet.transform.position = p;
            weapon.returnSpeed = 0.1f;
            weapon.RecallNow();
            for (int i = 0; i < 5; i++) Invoke(bullet, "StepReturning", 0.1f);
            Assert.That(target.ReturnHits, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Outbound_HitsTargetOnce_ThenDropsToFloor()
        {
            yield return Click();
            yield return Click();
            PGRecallBullet bullet = Bullet();
            bullet.enabled = false;
            PGDamageable target = Target("OutboundTest", new Vector3(12f, 1f, 2f));
            bullet.transform.position = new Vector3(12f, 1f, -2f);
            typeof(PGRecallBullet).GetField("direction", Private).SetValue(bullet, Vector3.forward);
            Invoke(bullet, "StepOutbound", 0.2f);
            Assert.That(target.OutboundHits, Is.EqualTo(1));
            Assert.That(bullet.State, Is.EqualTo(PGRecallBullet.FlightState.Falling));
            Assert.That(bullet.transform.position.z, Is.LessThan(1.5f));
            for (int i = 0; i < 60 && bullet.State == PGRecallBullet.FlightState.Falling; i++)
                Invoke(bullet, "StepFalling", 1f / 60f);
            Assert.That(bullet.State, Is.EqualTo(PGRecallBullet.FlightState.Anchored));
            Assert.That(bullet.transform.position.y, Is.GreaterThan(0f));
            Assert.That(weapon.Ammo, Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator Outbound_WallBlocksHighSpeedShot()
        {
            yield return Click();
            yield return Click();
            PGRecallBullet bullet = Bullet();
            bullet.enabled = false;
            bullet.transform.position = new Vector3(12f, 1.5f, 26f);
            typeof(PGRecallBullet).GetField("direction", Private).SetValue(bullet, Vector3.forward);
            Invoke(bullet, "StepOutbound", 1f);
            Assert.That(bullet.State, Is.EqualTo(PGRecallBullet.FlightState.Falling));
            Assert.That(bullet.transform.position.z, Is.LessThan(31f));
        }

        [UnityTest]
        public IEnumerator RecoilChangesRenderedCamera_ButNotLogicalAim()
        {
            yield return Click();
            yield return new WaitForSeconds(0.3f);
            rig.enabled = false;
            Ray before = rig.GetAimRay();
            Quaternion rotation = rig.ViewCamera.transform.rotation;
            rig.AddShotKick();
            Invoke(rig, "LateUpdate");
            Ray after = rig.GetAimRay();
            Assert.That(Vector3.Distance(before.origin, after.origin), Is.LessThan(0.00001f));
            Assert.That(Vector3.Angle(before.direction, after.direction), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(rotation, rig.ViewCamera.transform.rotation), Is.GreaterThan(1f));
        }

        [UnityTest]
        public IEnumerator SlideUsesAim_NotMove_CameraCanTurn_RecallFollowsMovingPlayer()
        {
            yield return Click();
            yield return Click();
            Bullet().transform.position = weapon.RecallPosition + Vector3.forward * 25f;
            weapon.returnSpeed = 8f;
            weapon.RecallNow();
            SetInput("Move", Vector2.left);
            Vector3 aim = rig.GetPlanarLook(motor.transform.position);
            Vector3 start = motor.transform.position;
            SetInput("SlidePressed", true);
            yield return null;
            SetInput("SlidePressed", false);
            Assert.That(motor.IsSliding, Is.True);
            Quaternion cameraStart = rig.FlatRotation;
            SetInput("LookDelta", new Vector2(10f, 0f));
            yield return new WaitForSeconds(0.15f);
            SetInput("LookDelta", Vector2.zero);
            Vector3 travel = motor.transform.position - start;
            travel.y = 0;
            Assert.That(Vector3.Angle(travel, aim), Is.LessThan(8f));
            Assert.That(Quaternion.Angle(cameraStart, rig.FlatRotation), Is.GreaterThan(1f));
            Assert.That(travel.magnitude, Is.GreaterThan(1f));
            SetInput("Move", Vector2.zero);
            weapon.returnSpeed = 100f;
            yield return UntilRecovered();
        }

        [UnityTest]
        public IEnumerator CameraRelativeMovement_SprintIsFaster_StopsSmoothly()
        {
            typeof(PGCameraRig).GetField("yaw", Private).SetValue(rig, 90f);
            SetInput("Move", Vector2.up);
            yield return new WaitForSeconds(0.2f);
            Vector3 start = motor.transform.position;
            yield return new WaitForSeconds(0.2f);
            Vector3 walk = motor.transform.position - start;
            Assert.That(walk.x, Is.GreaterThan(0.8f));
            Assert.That(Mathf.Abs(walk.z), Is.LessThan(0.15f));
            SetInput("Sprint", true);
            yield return new WaitForSeconds(0.1f);
            start = motor.transform.position;
            yield return new WaitForSeconds(0.2f);
            Assert.That((motor.transform.position - start).magnitude, Is.GreaterThan(walk.magnitude * 1.3f));
            SetInput("Move", Vector2.zero);
            yield return new WaitForSeconds(0.2f);
            start = motor.transform.position;
            yield return new WaitForSeconds(0.15f);
            Assert.That(Vector3.Distance(start, motor.transform.position), Is.LessThan(0.02f));
        }

        [UnityTest]
        public IEnumerator LowCeiling_PreventsStandingUntilClear()
        {
            CharacterController controller = motor.GetComponent<CharacterController>();
            controller.enabled = false;
            motor.transform.position = new Vector3(-8f, 0.03f, 2f);
            controller.height = motor.slideHeight;
            controller.center = Vector3.up * motor.slideHeight * 0.5f;
            controller.enabled = true;
            yield return null;
            yield return null;
            Assert.That(controller.height, Is.LessThan(1.1f));
            controller.enabled = false;
            motor.transform.position = new Vector3(-8f, 0.03f, -2f);
            controller.enabled = true;
            yield return null;
            yield return null;
            Assert.That(controller.height, Is.EqualTo(1.8f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator RemovedBullet_ReplacedAtFieldPosition_WithoutGrantingAmmo()
        {
            yield return Click();
            yield return Click();
            PGRecallBullet bullet = Bullet();
            int id = bullet.Id;
            bullet.transform.position = weapon.RecallPosition + Vector3.forward * 20f;
            weapon.returnSpeed = 1f;
            LogAssert.Expect(LogType.Warning, "An active revolver bullet was externally removed. Returning a replacement with the same chamber ID.");
            Object.Destroy(bullet.gameObject);
            yield return null;
            yield return null;
            Assert.That(Bullet().Id, Is.EqualTo(id));
            Assert.That(weapon.Ammo, Is.EqualTo(5));
            Assert.That(Vector3.Distance(Bullet().transform.position, weapon.RecallPosition), Is.GreaterThan(15f));
            Assert.That(weapon.IsRecalling, Is.True);
            weapon.returnSpeed = 100f;
            yield return UntilRecovered();
        }

        [UnityTest]
        public IEnumerator FastEnemyProjectile_CrossesReturnBetweenFrames_IsIntercepted()
        {
            yield return Click();
            yield return Click();
            PGRecallBullet bullet = Bullet();
            Vector3 hand = weapon.RecallPosition;
            bullet.transform.position = hand + Vector3.forward * 8f;
            weapon.returnSpeed = 40f;
            weapon.RecallNow();
            GameObject shot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shot.layer = LayerMask.NameToLayer("PG_EnemyBullet");
            shot.transform.position = hand + new Vector3(-4f, 0f, 4f);
            shot.GetComponent<Collider>().isTrigger = true;
            PGEnemyProjectile projectile = shot.AddComponent<PGEnemyProjectile>();
            projectile.Initialize(Vector3.right * 40f, 0, 0);
            Invoke(projectile, "Step", 0.2f);
            Assert.That(shot.GetComponent<Collider>().enabled, Is.False,
                "Crossing swept paths should intercept before either endpoint overlaps");
            yield return null;
            Assert.That(projectile == null, Is.True);
        }

        [UnityTest]
        public IEnumerator SlideDistance_IsConsistentAt30_60_120Fps()
        {
            foreach (int fps in new[] { 30, 60, 120 })
            {
                Time.captureDeltaTime = 1f / fps;
                CharacterController cc = motor.GetComponent<CharacterController>();
                cc.enabled = false;
                motor.transform.position = new Vector3(0f, 0.02f, -4f);
                cc.enabled = true;
                typeof(PGPlayerMotor).GetField("nextSlideTime", Private).SetValue(motor, 0f);
                yield return new WaitForSeconds(0.2f);
                Vector3 start = motor.transform.position;
                SetInput("SlidePressed", true);
                yield return null;
                SetInput("SlidePressed", false);
                Assert.That(motor.IsSliding, Is.True);
                while (motor.IsSliding) yield return null;
                Vector3 travel = motor.transform.position - start;
                travel.y = 0f;
                float expected = motor.slideSpeed * motor.slideDuration * (1f + motor.slideEndSpeedRatio) * 0.5f;
                Assert.That(travel.magnitude, Is.EqualTo(expected).Within(0.12f), "FPS: " + fps);
                yield return new WaitForSeconds(0.3f);
            }
        }

        [UnityTest]
        public IEnumerator Hud_SelectsChildCollider_TracksHealth_AndClearsDeadTarget()
        {
            yield return Click();
            yield return new WaitForSeconds(0.3f);
            PGPrototypeHUD hud = Object.FindFirstObjectByType<PGPrototypeHUD>();
            PGDamageable target = Target("HUDTarget", rig.GetAimRay().GetPoint(5f), true);
            target.GetComponent<Collider>().enabled = false; // Aim must resolve the child's owner.
            Invoke(hud, "LateUpdate");
            Assert.That(hud.CurrentTarget, Is.SameAs(target));
            target.TakeDamage(20, false);
            Invoke(hud, "LateUpdate");
            Assert.That(hud.CurrentTarget.Health, Is.EqualTo(130));
            target.TakeDamage(1000, true);
            Invoke(hud, "LateUpdate");
            Assert.That(hud.CurrentTarget, Is.Null);
        }

        [UnityTest]
        public IEnumerator Hud_WallAndEmptyAim_ClearPanel_AndCancelAimHidesIt()
        {
            yield return Click();
            yield return new WaitForSeconds(0.3f);
            PGPrototypeHUD hud = Object.FindFirstObjectByType<PGPrototypeHUD>();
            Ray ray = rig.GetAimRay();
            PGDamageable target = Target("HUDTarget", ray.GetPoint(5f));
            Invoke(hud, "LateUpdate");
            Assert.That(hud.CurrentTarget, Is.SameAs(target));
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = ray.GetPoint(3f);
            Invoke(hud, "LateUpdate");
            Assert.That(hud.CurrentTarget, Is.Null, "A wall must hide the enemy's health panel");
            wall.SetActive(false);
            target.gameObject.SetActive(false);
            Invoke(hud, "LateUpdate");
            Assert.That(hud.CurrentTarget, Is.Null);
            target.gameObject.SetActive(true);
            Invoke(hud, "LateUpdate");
            Assert.That(hud.CurrentTarget, Is.SameAs(target));
            yield return Click(true);
            Assert.That(hud.CurrentTarget, Is.Null);
        }

        [UnityTest]
        public IEnumerator Hud_UsesFreeCursorRay_AndIgnoresVisualRecoil()
        {
            yield return Click();
            yield return new WaitForSeconds(0.3f);
            PGPrototypeHUD hud = Object.FindFirstObjectByType<PGPrototypeHUD>();
            input.freeCursorAim = true;
            SetInput("Pointer", new Vector2(Screen.width * 0.7f, Screen.height * 0.6f));
            PGDamageable target = Target("CursorTarget", rig.GetAimRay().GetPoint(5f));
            Invoke(hud, "LateUpdate");
            Assert.That(hud.CurrentTarget, Is.SameAs(target));
            rig.AddShotKick();
            Invoke(rig, "LateUpdate");
            Invoke(hud, "LateUpdate");
            Assert.That(hud.CurrentTarget, Is.SameAs(target));
            SetInput("Active", false);
            Invoke(hud, "LateUpdate");
            Assert.That(hud.CurrentTarget, Is.Null);
        }

        [UnityTest]
        public IEnumerator SlideVisualPose_TiltsAndRestores_WithoutTiltingController()
        {
            Transform pose = motor.visualPose;
            Vector3 originalScale = pose.localScale;
            Quaternion originalRotation = pose.localRotation;
            SetInput("SlidePressed", true);
            yield return null;
            SetInput("SlidePressed", false);
            yield return new WaitForSeconds(0.1f);
            Assert.That(motor.IsSliding, Is.True);
            Assert.That(pose.localScale.y, Is.LessThan(originalScale.y * 0.8f));
            Assert.That(Quaternion.Angle(pose.localRotation, originalRotation), Is.GreaterThan(8f));
            Assert.That(Vector3.Angle(motor.transform.up, Vector3.up), Is.LessThan(0.01f));
            yield return new WaitForSeconds(0.6f);
            Assert.That(motor.IsSliding, Is.False);
            Assert.That(Quaternion.Angle(pose.localRotation, originalRotation), Is.LessThan(0.1f));
            Assert.That(Vector3.Distance(pose.localScale, originalScale), Is.LessThan(0.01f));
        }

        private T CameraState<T>(string field) => (T)typeof(PGCameraRig).GetField(field, Private).GetValue(rig);

        [UnityTest]
        public IEnumerator RecallFeedback_FollowsReturn_PreservesAim_StopsOnArrival()
        {
            yield return Click(true); // Empty cylinder field: no recall shake.
            Assert.That(CameraState<bool>("recallPullActive"), Is.False);
            yield return Click();
            yield return Click();
            Bullet().transform.position = weapon.RecallPosition + Vector3.forward * 20f;
            weapon.returnSpeed = 1f;
            yield return Click(true);
            yield return new WaitForSeconds(0.15f);
            Assert.That(CameraState<bool>("recallPullActive"), Is.True);
            Assert.That(CameraState<float>("recallPullBlend"), Is.EqualTo(1f).Within(0.001f));
            Ray before = rig.GetAimRay();
            Invoke(rig, "LateUpdate");
            Vector3 shakenPosition = rig.ViewCamera.transform.position;
            Quaternion shakenRotation = rig.ViewCamera.transform.rotation;
            rig.motionScale = 0f;
            Invoke(rig, "LateUpdate");
            float displacement = Vector3.Distance(shakenPosition, rig.ViewCamera.transform.position);
            float angle = Quaternion.Angle(shakenRotation, rig.ViewCamera.transform.rotation);
            Assert.That(displacement + angle, Is.GreaterThan(0.01f));
            Ray after = rig.GetAimRay();
            Assert.That(Vector3.Distance(before.origin, after.origin), Is.LessThan(0.00001f));
            Assert.That(Vector3.Angle(before.direction, after.direction), Is.LessThan(0.001f));
            rig.motionScale = 1f;
            weapon.returnSpeed = 100f;
            yield return UntilRecovered();
            Assert.That(CameraState<bool>("recallPullActive"), Is.False);
            yield return new WaitForSeconds(rig.recallShakeRelease + rig.recallHitShakeSeconds + 0.1f);
            Assert.That(CameraState<float>("recallPullBlend"), Is.Zero);
            Assert.That(CameraState<float>("recallHitLeft"), Is.Zero);
        }

        [UnityTest]
        public IEnumerator RecallHitFeedback_KillingBlowTriggersOnce_ForMultipleColliders()
        {
            yield return Click();
            yield return Click();
            PGRecallBullet bullet = Bullet();
            bullet.enabled = false;
            Vector3 position = weapon.RecallPosition + Vector3.forward * 5f;
            PGDamageable target = Target("RecallImpactTarget", position, true);
            bullet.transform.position = position;
            weapon.returnSpeed = 0.1f;
            weapon.returnDamage = 1000;
            weapon.RecallNow();
            Assert.That(CameraState<float>("recallHitAmplitude"), Is.Zero);
            Invoke(bullet, "StepReturning", 0.1f);
            Assert.That(target.Health, Is.Zero);
            Assert.That(CameraState<float>("recallHitAmplitude"), Is.EqualTo(1f));
            Assert.That(CameraState<float>("recallHitLeft"), Is.GreaterThan(0f));
            for (int i = 0; i < 5; i++) Invoke(bullet, "StepReturning", 0.1f);
            Assert.That(CameraState<float>("recallHitAmplitude"), Is.EqualTo(1f),
                "Neither duplicate colliders nor a dead target may retrigger impact");
        }

        [UnityTest]
        public IEnumerator RecallHitFeedback_NoDamageIsSilent_StackIsBounded_MotionScaleMutes()
        {
            yield return Click();
            yield return Click();
            yield return new WaitForSeconds(0.4f);
            PGRecallBullet bullet = Bullet();
            bullet.enabled = false;
            Vector3 position = weapon.RecallPosition + Vector3.forward * 5f;
            PGDamageable target = Target("ZeroDamageTarget", position, true);
            bullet.transform.position = position;
            weapon.returnSpeed = 0.1f;
            weapon.returnDamage = 0;
            weapon.RecallNow();
            Invoke(bullet, "StepReturning", 0.1f);
            Assert.That(target.Health, Is.EqualTo(target.maxHealth));
            Assert.That(CameraState<float>("recallHitLeft"), Is.Zero);
            rig.motionScale = 0f;
            Invoke(rig, "LateUpdate");
            Vector3 baselinePosition = rig.ViewCamera.transform.position;
            Quaternion baselineRotation = rig.ViewCamera.transform.rotation;
            Ray aim = rig.GetAimRay();
            for (int i = 0; i < 12; i++) rig.AddRecallHitKick();
            Assert.That(CameraState<float>("recallHitAmplitude"), Is.EqualTo(rig.recallHitMaxStack));
            Invoke(rig, "LateUpdate");
            Assert.That(Vector3.Distance(baselinePosition, rig.ViewCamera.transform.position), Is.LessThan(0.00001f));
            Assert.That(Quaternion.Angle(baselineRotation, rig.ViewCamera.transform.rotation), Is.LessThan(0.001f));
            rig.motionScale = 1f;
            Invoke(rig, "LateUpdate");
            Assert.That(Quaternion.Angle(baselineRotation, rig.ViewCamera.transform.rotation), Is.GreaterThan(0.1f));
            Assert.That(Vector3.Angle(aim.direction, rig.GetAimRay().direction), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(aim.origin, rig.GetAimRay().origin), Is.LessThan(0.00001f));
            yield return new WaitForSeconds(rig.recallHitShakeSeconds + 0.1f);
            Assert.That(CameraState<float>("recallHitLeft"), Is.Zero);
        }

        [UnityTest]
        public IEnumerator RecallFeedback_AutoDelayWaits_WeaponDisableAndResumeFollowState()
        {
            weapon.autoRecallDelay = 0.5f;
            weapon.returnSpeed = 0.1f;
            yield return Click();
            for (int i = 0; i < 6; i++) yield return Click();
            Assert.That(weapon.Phase, Is.EqualTo(PGRevolver.CyclePhase.RecallDelay));
            Assert.That(CameraState<bool>("recallPullActive"), Is.False);
            foreach (PGRecallBullet bullet in Object.FindObjectsByType<PGRecallBullet>(FindObjectsSortMode.None))
                bullet.transform.position = weapon.RecallPosition + Vector3.forward * 15f;
            yield return new WaitForSeconds(0.6f);
            Assert.That(weapon.IsAiming, Is.True);
            Assert.That(CameraState<bool>("recallPullActive"), Is.True);
            weapon.enabled = false;
            Assert.That(CameraState<bool>("recallPullActive"), Is.False);
            yield return new WaitForSeconds(rig.recallShakeRelease + 0.1f);
            Assert.That(CameraState<float>("recallPullBlend"), Is.Zero);
            weapon.enabled = true;
            Assert.That(CameraState<bool>("recallPullActive"), Is.True);
            weapon.returnSpeed = 100f;
            yield return UntilRecovered();
            Assert.That(CameraState<bool>("recallPullActive"), Is.False);
        }
    }
}
