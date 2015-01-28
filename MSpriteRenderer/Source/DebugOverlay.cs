using Mogre;

namespace MSpriteRenderer
{

    public class DebugOverlay
    {
        TextManager mTextManager;

        Text mFPSAvg;
        Text mFPSCurr;
        Text mFPSBest;
        Text mFPSWorst;
        Text mTris;
        Text mModes;
        public DebugOverlay(RenderWindow window)
        {
            mWindow = window;
            mTextManager = new TextManager();

            mFPSAvg = new Text("Average FPS:") { Position = new System.Drawing.Point(10, 0) };
            mFPSCurr = new Text("Current FPS:") { Position = new System.Drawing.Point(10, 15) };
            mFPSBest = new Text("Best FPS:") { Position = new System.Drawing.Point(10, 30) };
            mFPSWorst = new Text("Worst FPS:") { Position = new System.Drawing.Point(10, 45) };
            mTris = new Text("Triangle Count:") { Position = new System.Drawing.Point(10, 60) };
            mModes = new Text("???") { Position = new System.Drawing.Point(10, 75) };

            mTextManager.AddText(mFPSAvg);
            mTextManager.AddText(mFPSCurr);
            mTextManager.AddText(mFPSBest);
            mTextManager.AddText(mFPSWorst);
            mTextManager.AddText(mTris);
            mTextManager.AddText(mModes);

            mTextManager.ShowAllText();
        }

        public string AdditionalInfo
        {
            set { mAdditionalInfo = value; }
            get { return mAdditionalInfo; }
        }

        public void Update(float timeFragment)
        {
            if (timeSinceLastDebugUpdate > 0.5f)
            {
                var stats = mWindow.GetStatistics();

                mFPSAvg.Caption= "Average FPS: " + stats.AvgFPS;
                mFPSCurr.Caption = "Current FPS: " + stats.LastFPS;
                mFPSBest.Caption = "Best FPS: " + stats.BestFPS + " " + stats.BestFrameTime + " ms";
                mFPSWorst.Caption = "Worst FPS: " + stats.WorstFPS + " " + stats.WorstFrameTime + " ms";
                mTris.Caption = "Triangle Count: " + stats.TriangleCount;
                mModes.Caption = mAdditionalInfo;

                timeSinceLastDebugUpdate = 0;
            }
            else
            {
                timeSinceLastDebugUpdate += timeFragment;
            }
        }

        protected RenderWindow mWindow;
        protected float timeSinceLastDebugUpdate = 1;
        protected OverlayElement mGuiAvg;
        protected OverlayElement mGuiCurr;
        protected OverlayElement mGuiBest;
        protected OverlayElement mGuiWorst;
        protected OverlayElement mGuiTris;
        protected OverlayElement mModesText;
        protected string mAdditionalInfo = "";

    }

}