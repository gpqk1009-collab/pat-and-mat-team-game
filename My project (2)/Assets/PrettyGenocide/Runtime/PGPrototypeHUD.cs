using System;
using UnityEngine;

namespace PrettyGenocide
{
    public sealed class PGPrototypeHUD : MonoBehaviour
    {
        [Serializable]
        public sealed class HUDText
        {
            public string title = "프리티 제노사이드 / 전투 시험장";
            public string ammo = "장탄";
            public string fieldBullets = "필드 탄환";
            public string playerHealth = "내 체력";
            public string hip = "대기 · 좌클릭으로 조준";
            public string aiming = "조준 중";
            public string recallDelay = "자동 회수 준비 중";
            public string recalling = "탄환 회수 중 · 발사 불가";
            public string sliding = "슬라이딩 중";
            public string slideReady = "슬라이딩 준비 · Space";
            public string slideCooldown = "슬라이딩 대기";
            public string crouched = "낮은 자세 · 천장을 벗어나세요";
            public string seconds = "초";
            public string fireHint = "좌클릭: 조준 / 발사    우클릭: 조준 해제 + 회수";
            public string moveHint = "WASD: 이동    Shift: 달리기    Space: 슬라이딩";
            public string enemyFireHint = "Tab: 조준 방식    F2: 포탑 발사";
            public string on = "켜짐";
            public string off = "꺼짐";
            [TextArea] public string freeCursorHint = "마우스: 커서 조준 / 가운데 버튼 누르고 이동: 카메라 회전 / Esc: 조작 해제";
            [TextArea] public string shoulderHint = "마우스: 카메라 회전 / 화면 중앙으로 조준 / Esc: 조작 해제";
            public string resume = "게임 화면을 클릭하면 조작을 재개합니다";
            public string targetHeading = "조준 대상";
            public string unnamedTarget = "적";
            public string targetHealth = "체력";
            public string outboundHits = "일반 피격";
            public string returnHits = "회수 피격";
        }

        [Header("References")]
        public PGInput input;
        public PGPlayerMotor player;
        public PGRevolver revolver;
        public PGCameraRig cameraRig;
        public PGTrainingTurret[] turrets;

        [Header("게임 문구 · Play를 끈 상태에서 수정하고 씬 저장")]
        public HUDText texts = new HUDText();
        [Tooltip("한글 TTF/OTF 폰트를 연결할 수 있습니다. 비워 두면 OS의 한글 폰트를 사용합니다.")]
        public Font hudFont;

        [Header("오른쪽 조준 대상 체력")]
        public bool onlyWhileAiming = true;
        public bool showHitCounts;
        [Min(180f)] public float targetPanelWidth = 280f;
        [Tooltip("X: 오른쪽 여백, Y: 화면 중앙으로부터 아래쪽 이동")]
        public Vector2 targetPanelOffset = new Vector2(20f, 0f);
        public Color healthColor = new Color(1f, 0.28f, 0.60f);
        public Color panelColor = new Color(0.07f, 0.06f, 0.12f, 0.92f);

        public PGDamageable CurrentTarget { get; private set; }
        private bool attacksEnabled;
        private Font runtimeFont;
        private Font styledFont;
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

        private void LateUpdate()
        {
            CurrentTarget = null;
            if (cameraRig == null || input == null || !input.Active ||
                (onlyWhileAiming && (revolver == null || !revolver.IsAiming))) return;
            // Share the revolver's unshaken ray; world geometry blocks target selection.
            Physics.SyncTransforms();
            if (!cameraRig.TryGetAimHit(out RaycastHit hit)) return;
            PGDamageable target = hit.collider.GetComponentInParent<PGDamageable>();
            if (target != null && target.isActiveAndEnabled && target.IsAlive) CurrentTarget = target;
        }

        private void EnsureStyles()
        {
            if (hudFont == null && runtimeFont == null)
                runtimeFont = Font.CreateDynamicFontFromOSFont(new[]
                {
                    "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Noto Sans KR"
                }, 18);
            Font font = hudFont != null ? hudFont : runtimeFont;
            if (titleStyle != null && styledFont == font) return;
            styledFont = font;
            titleStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 21, fontStyle = FontStyle.Bold };
            textStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 16 };
            smallStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 13, wordWrap = true };
            titleStyle.normal.textColor = textStyle.normal.textColor = smallStyle.normal.textColor = Color.white;
        }

        private void OnGUI()
        {
            if (revolver == null || player == null || input == null) return;
            EnsureStyles();
            float scale = Mathf.Max(0.01f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            GUI.color = Color.white;
            try
            {
                DrawPanel(new Rect(16f, 16f, 420f, 240f), panelColor);
                GUI.Label(new Rect(28f, 25f, 395f, 30f), texts.title, titleStyle);
                string mode = revolver.IsRecalling
                    ? (revolver.Phase == PGRevolver.CyclePhase.RecallDelay ? texts.recallDelay : texts.recalling)
                    : (revolver.IsAiming ? texts.aiming : texts.hip);
                GUI.Label(new Rect(28f, 61f, 395f, 25f), texts.ammo + " " + revolver.Ammo + " / 6    " +
                    texts.fieldBullets + " " + revolver.FieldBullets, textStyle);
                GUI.Label(new Rect(28f, 89f, 395f, 25f), mode, textStyle);
                GUI.Label(new Rect(28f, 117f, 395f, 25f), texts.playerHealth + " " + player.Health + " / " + player.maxHealth, textStyle);
                string slide = player.IsSliding ? texts.sliding : player.IsCrouched ? texts.crouched :
                    player.SlideCooldownRemaining > 0f
                        ? texts.slideCooldown + " " + player.SlideCooldownRemaining.ToString("0.0") + texts.seconds
                        : texts.slideReady;
                GUI.Label(new Rect(28f, 145f, 395f, 25f), slide, textStyle);
                GUI.Label(new Rect(28f, 175f, 395f, 23f), texts.fireHint, smallStyle);
                GUI.Label(new Rect(28f, 198f, 395f, 23f), texts.moveHint, smallStyle);
                GUI.Label(new Rect(28f, 221f, 395f, 23f), texts.enemyFireHint + " " + (attacksEnabled ? texts.on : texts.off), smallStyle);
                GUI.Label(new Rect(20f, height - 52f, width - 40f, 44f),
                    input.freeCursorAim ? texts.freeCursorHint : texts.shoulderHint, smallStyle);
                if (!input.Active)
                {
                    Rect resumeRect = new Rect(width / 2f - 200f, height / 2f - 28f, 400f, 56f);
                    DrawPanel(resumeRect, panelColor);
                    GUI.Label(new Rect(resumeRect.x + 16f, resumeRect.y + 17f, 368f, 28f), texts.resume, textStyle);
                }
                else
                {
                    Vector2 p = input.Pointer / scale;
                    float y = height - p.y;
                    Color crosshair = revolver.IsRecalling ? healthColor : Color.white;
                    DrawPanel(new Rect(p.x - 8f, y - 1f, 16f, 2f), crosshair);
                    DrawPanel(new Rect(p.x - 1f, y - 8f, 2f, 16f), crosshair);
                }
                if (CurrentTarget != null && CurrentTarget.IsAlive) DrawTargetPanel(width, height);
            }
            finally
            {
                GUI.matrix = oldMatrix;
                GUI.color = oldColor;
            }
        }

        private void DrawTargetPanel(float width, float height)
        {
            float panelWidth = Mathf.Clamp(targetPanelWidth, 180f, width * 0.4f);
            float panelHeight = showHitCounts ? 160f : 134f;
            float x = Mathf.Clamp(width - panelWidth - targetPanelOffset.x, 0f, width - panelWidth);
            float y = Mathf.Clamp((height - panelHeight) * 0.5f + targetPanelOffset.y, 0f, height - panelHeight);
            DrawPanel(new Rect(x, y, panelWidth, panelHeight), panelColor);
            GUI.Label(new Rect(x + 16f, y + 10f, panelWidth - 32f, 22f), texts.targetHeading, smallStyle);
            string name = string.IsNullOrWhiteSpace(CurrentTarget.displayName) ? texts.unnamedTarget : CurrentTarget.displayName;
            GUI.Label(new Rect(x + 16f, y + 34f, panelWidth - 32f, 30f), name, titleStyle);
            GUI.Label(new Rect(x + 16f, y + 68f, panelWidth - 32f, 25f), texts.targetHealth + " " +
                CurrentTarget.Health + " / " + CurrentTarget.maxHealth, textStyle);
            Rect bar = new Rect(x + 16f, y + 103f, panelWidth - 32f, 12f);
            DrawPanel(bar, new Color(0.23f, 0.20f, 0.28f));
            float fraction = Mathf.Clamp01(CurrentTarget.Health / (float)Mathf.Max(1, CurrentTarget.maxHealth));
            DrawPanel(new Rect(bar.x, bar.y, bar.width * fraction, bar.height), healthColor);
            if (showHitCounts)
                GUI.Label(new Rect(x + 16f, y + 126f, panelWidth - 32f, 25f), texts.outboundHits + " " +
                    CurrentTarget.OutboundHits + "    " + texts.returnHits + " " + CurrentTarget.ReturnHits, smallStyle);
        }

        private static void DrawPanel(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void OnDestroy()
        {
            if (runtimeFont != null) Destroy(runtimeFont);
        }
    }
}
