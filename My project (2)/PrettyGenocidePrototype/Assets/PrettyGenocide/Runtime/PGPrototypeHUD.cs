using UnityEngine;

namespace PrettyGenocide
{
    public sealed class PGPrototypeHUD : MonoBehaviour
    {
        public PGInput input;
        public PGPlayerMotor player;
        public PGRevolver revolver;
        public PGCameraRig cameraRig;
        public PGDamageable[] targets;
        public PGTrainingTurret[] turrets;
        private bool attacksEnabled;
        private GUIStyle titleStyle;
        private GUIStyle textStyle;
        private GUIStyle smallStyle;

        private void Update()
        {
            if (input != null && input.ToggleEnemiesPressed)
            {
                attacksEnabled = !attacksEnabled;
                if (turrets != null)
                    foreach (PGTrainingTurret turret in turrets)
                        if (turret != null) turret.attacksEnabled = attacksEnabled;
            }
        }

        private void OnGUI()
        {
            if (revolver == null || player == null || input == null) return;
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 21, fontStyle = FontStyle.Bold };
                textStyle = new GUIStyle(GUI.skin.label) { fontSize = 16 };
                smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            }
            GUI.Box(new Rect(14f, 14f, 415f, 216f), GUIContent.none);
            GUI.Label(new Rect(26f, 24f, 395f, 30f), "PRETTY GENOCIDE / PROTOTYPE", titleStyle);
            string mode = revolver.IsRecalling ? revolver.Phase.ToString().ToUpperInvariant()
                : (revolver.IsAiming ? "AIMING" : "HIP / FIRST CLICK TO AIM");
            GUI.Label(new Rect(26f, 60f, 395f, 26f), "AMMO " + revolver.Ammo + " / 6    FIELD " + revolver.FieldBullets, textStyle);
            GUI.Label(new Rect(26f, 89f, 395f, 25f), mode, textStyle);
            GUI.Label(new Rect(26f, 118f, 395f, 25f), "HP " + player.Health + "    SLIDE CD " + player.SlideCooldownRemaining.ToString("0.0"), textStyle);
            GUI.Label(new Rect(26f, 148f, 395f, 24f), "LMB: aim, then fire   |   RMB: cancel + recall", smallStyle);
            GUI.Label(new Rect(26f, 171f, 395f, 24f), "WASD: move   Shift: sprint   Space: slide", smallStyle);
            GUI.Label(new Rect(26f, 194f, 395f, 24f), "Tab: aim mode   F2: enemy fire " + (attacksEnabled ? "ON" : "OFF"), smallStyle);
            GUI.Label(new Rect(18f, Screen.height - 50f, 820f, 25f), input.freeCursorAim
                ? "FREE CURSOR: point to aim; hold MIDDLE MOUSE to orbit. Escape: release controls."
                : "SHOULDER VIEW: move mouse to look. Escape: release cursor.", smallStyle);
            if (!input.Active)
                GUI.Box(new Rect(Screen.width / 2f - 155f, Screen.height / 2f - 25f, 310f, 50f), "CLICK GAME VIEW TO RESUME");

            if (input.Active)
            {
                Vector2 p = input.Pointer;
                float x = p.x;
                float y = Screen.height - p.y;
                Color previous = GUI.color;
                GUI.color = revolver.IsRecalling ? new Color(1f, 0.35f, 0.65f) : Color.white;
                GUI.DrawTexture(new Rect(x - 8f, y - 1f, 16f, 2f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x - 1f, y - 8f, 2f, 16f), Texture2D.whiteTexture);
                GUI.color = previous;
            }
            if (targets == null || cameraRig == null || cameraRig.ViewCamera == null) return;
            foreach (PGDamageable target in targets)
            {
                if (target == null) continue;
                Vector3 screen = cameraRig.ViewCamera.WorldToScreenPoint(target.transform.position + Vector3.up * 2.25f);
                if (screen.z <= 0f) continue;
                string text = target.IsAlive ? "HP " + target.Health + " | OUT " + target.OutboundHits + " RET " + target.ReturnHits : "RESPAWNING";
                GUI.Label(new Rect(screen.x - 95f, Screen.height - screen.y, 210f, 24f), text, smallStyle);
            }
        }
    }
}
