using Blish_HUD;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

// Audio compass for the blind. Module for the Guild Wars 2 add-on Blish HUD: https://blishhud.com/

namespace Audio_Compass
{
    [Export(typeof(Blish_HUD.Modules.Module))]
    public class Module : Blish_HUD.Modules.Module
    {
        private static readonly Logger Logger = Logger.GetLogger<Module>();

        private class CPoint // cardinal point settings, data and processing
        {
            public static SettingEntry<float> SuppressionAngle; // distance from a cardinal point within which repetition is suppressed
            public float Angle; // from North
            public string Name; // for SettingEntry ID
            public int DefFreq; // defaults
            public int DefDur;
            public enum EAction { Silent, Beep }
            public SettingEntry<EAction> Action;
            public SettingEntry<int> BeepFrequency;
            public SettingEntry<int> BeepDuration;
            public bool SuppressionSentry; // for repetition suppression

            public CPoint(float angle, string name, int deffreq, int defdur)
            {
                Angle = angle;
                Name = name;
                DefFreq = deffreq;
                DefDur = defdur;
                SuppressionSentry = false;
            }

            public void DefineSettings(SettingCollection settings)
            {
                Action = settings.DefineSetting($"{Name}Action", EAction.Beep, () => $"{Name} action");
                BeepFrequency = settings.DefineSetting($"{Name}BeepFrequency", DefFreq, () => $"{Name} beep frequency");
                BeepFrequency.SetRange(300, 8000);
                BeepDuration = settings.DefineSetting($"{Name}BeepDuration", DefDur, () => $"{Name} beep duration");
                BeepDuration.SetRange(100, 500);
            }

            protected float NormalizeAngle(float angle) // normalize for the cardinal point to be at 0, makes for easier math
            {
                angle -= Angle;
                if (angle <= -180f) angle += 360f; else if (angle > 180f) angle -= 360f;
                return angle;
            }

            public void GoBeep(float dir, float prevdir)
            {
                if (Action.Value == EAction.Silent) return;

                dir = NormalizeAngle(dir);
                prevdir = NormalizeAngle(prevdir);
                float absdir = Math.Abs(dir);
                if (absdir > SuppressionAngle.Value) SuppressionSentry = false;
                if (SuppressionSentry) return;

                if (Math.Sign(dir) != Math.Sign(prevdir) && absdir < 90f) // passing a cardinal point and not leaping over the opposite one
                {
                    Console.Beep(BeepFrequency.Value, BeepDuration.Value); // TODO: use a helper thread to make this asynchronous
                    SuppressionSentry = true;
                }
            }
        }

        private SettingEntry<bool> SuppressInCombat;
        //private SettingEntry<float> SuppressionAngle; // distance from a cardinal point within which repetition is suppressed
        private CPoint[] CPoints;

        private float CamDirection;
        private float PrevCamDirection;

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        [ImportingConstructor]
        public Module([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
        {
            CPoints = new CPoint[4]
            {
                new CPoint(  0f, "North", 2000, 300),
                new CPoint( 90f, "East",  1500, 100),
                new CPoint(180f, "South", 1000, 300),
                new CPoint(-90f, "West",   500, 100)
            };
            CamDirection = 0f;
            PrevCamDirection = 0f;
        }

        protected override void DefineSettings(SettingCollection settings)
        {
            SuppressInCombat = settings.DefineSetting("SuppressInCombat", false, () => "Suppress when in combat");
            CPoint.SuppressionAngle = settings.DefineSetting("SuppressionAngle", 45f, () => "Suppression angle");
            CPoint.SuppressionAngle.SetRange(0f, 45f);
            for (int c = 0; c < 4; c++) CPoints[c].DefineSettings(settings); // would use foreach but not sure if C# would provide refs
        }

        protected override void Initialize()
        {

        }

        protected override async Task LoadAsync()
        {

        }

        protected override void OnModuleLoaded(EventArgs e)
        {

            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        protected void GoBeep()
        {
            if (SuppressInCombat.Value && GameService.Gw2Mumble.PlayerCharacter.IsInCombat) return;

            for (int c = 0; c < 4; c++) CPoints[c].GoBeep(CamDirection, PrevCamDirection); // would use foreach but not sure if C# would provide refs

            //for (int c = 0; c < 4; c++) // would use foreach but not sure if C# would provide refs
            //{
            //    if (CPoints[c].Action.Value == CPoint.EAction.Silent) continue;
                
            //    float dir = ShiftAngle(CamDirection, -CPoints[c].Angle); // normalize for the cardinal point to be at 0, makes for easier math
            //    float prevdir = ShiftAngle(PrevCamDirection, -CPoints[c].Angle);
            //    float absdir = Math.Abs(dir);
            //    if (absdir > SuppressionAngle.Value) CPoints[c].SuppressionSentry = false;
            //    if (CPoints[c].SuppressionSentry) continue;
                
            //    if (Math.Sign(dir) != Math.Sign(prevdir) && absdir < 90f) // passing a cardinal point and not leaping over the opposite one
            //    {
            //        Console.Beep(CPoints[c].BeepFrequency.Value, CPoints[c].BeepDuration.Value); // TODO: use a helper thread to make this asynchronous
            //        CPoints[c].SuppressionSentry = true;
            //    }
            //}
        }

        protected override void Update(GameTime gameTime)
        {
            PrevCamDirection = CamDirection;
            Vector3 PCAt = GameService.Gw2Mumble.PlayerCamera.Forward; // Forward is mapped XZY from the Mumble Link vector
            CamDirection = (float)(Math.Atan2(PCAt.X, PCAt.Y) * 180.0 / Math.PI);
            if (CamDirection == PrevCamDirection) return;
            GoBeep();
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            // Unload here

            // All static members must be manually unset
        }

    }

}
