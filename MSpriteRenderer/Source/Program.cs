using Mogre;
using MSpriteRenderer.Source;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using PixelFormat = Mogre.PixelFormat;

namespace MSpriteRenderer
{
    internal class Program : BaseApplication
    {
        /* This needs a major fucking cleanup. */
        private const uint CUSTOM_SHININESS = 1;
        private const uint CUSTOM_DIFFUSE = 2;
        private const uint CUSTOM_SPECULAR = 3;

        private RenderTexture mRTT;
        private Viewport mRTVP;
        private Vector3 _sceneCenter = Vector3.ZERO;

        // Entities
        private Entity _playerEntity;
        private Entity _currentItem;
        private SceneNode _playerNode;

        // Camera shit
        private int mCameraPosition;
        private List<Vector3> mCameraPositions;
        private List<string> mCameraDirections;
        private const float mCameraAngle = 45f; // Angle from the target.
        private float mDistance = 42.426f; //30f for the old system
        private float mPythDistance;

        // Task scheduling shit
        private LimitedConcurrencyLevelTaskScheduler imageTrimScheduler;
        private TaskFactory imageTrimTaskFactory;
        private Queue<RenderTask> renderTasks = new Queue<RenderTask>();
        private RenderTask currentRenderTask;

        // Animation shit
        private int mAnimationFramesPerSecond = 15;
        private float mAnimTime;
        private float mAnimMaxTime;
        private int mFrameCount = 0;
        //private List<string> _meshes; // Unused
        //private int _currentMeshIndex; // Unused
        private AnimationStateIterator mStateIterator;
        private AnimationState mCurrentAnimState;
        private List<string> mRenderedAnims = new List<string>(); //because mogre is a piece of shit 
        private AnimationCollection mAnimationCollection;
        private AnimationInfo mAnimationInfo;

        private Dictionary<string, PlayerModelConfig> mPlayerModelConfigs = new Dictionary<string, PlayerModelConfig>();
        private Dictionary<string, ItemConfig> mItemConfigs = new Dictionary<string, ItemConfig>();
        private Dictionary<string, WearableConfig> mWearableConfigs = new Dictionary<string, WearableConfig>();

        private static void Main(string[] args)
        {
            using (var program = new Program())
            {
                program.Go();
            }
        }

        /// <summary>
        /// Get a point on the surface of a sphere.
        /// </summary>
        /// <param name="radius">Radius</param>
        /// <param name="azimuth">Angle around the z-axis (from "north")</param>
        /// <param name="altitude">Height angle</param>
        /// <returns></returns>
        private Vector3 getPosOnSphere(float radius, Degree azimuth, Degree altitude)
        {
            return new Vector3(
                radius * Mogre.Math.Sin(azimuth) * Mogre.Math.Cos(altitude),  // East
                radius * (-Mogre.Math.Sin(altitude)), // Zenith
                radius * (-Mogre.Math.Cos(azimuth)) * Mogre.Math.Cos(altitude)  // North 
            );
        }

        protected override bool Setup()
        {
            ReadConfigs();
            CreateTasks();

            var setup = base.Setup();
            var ptr = TextureManager.Singleton.CreateManual("RttTex",
                                                            ResourceGroupManager.DEFAULT_RESOURCE_GROUP_NAME,
                                                            TextureType.TEX_TYPE_2D,
                                                            148,
                                                            148,
                                                            0,
                                                            PixelFormat.PF_R8G8B8A8,
                                                            (int)TextureUsage.TU_RENDERTARGET
                );
            mRTT = ptr.GetBuffer().GetRenderTarget();
            mRTVP = mRTT.AddViewport(mCamera);
            mRTVP.BackgroundColour = new ColourValue(0, 0, 0, 0);
            mRTVP.SetClearEveryFrame(true);
            mRTVP.OverlaysEnabled = false;

            //Calculate diagonal distance value
            //mPythDistance = (mDistance / Mogre.Math.Sqrt(2));

            var altitude = new Degree(mCameraAngle);
            var angles = new float[]{
                180f, // South
                135f, // Southeast
                 90f, // East
                 45f, // Northeast
                  0f, // North
                -45f, // Northwest
                -90f, // West
               -135f, // Southwest
            };
            mCameraDirections = new List<string> {
                "s",
                "se",
                "e",
                "ne",
                "n",
                "nw",
                "w",
                "sw"
            };
            mCameraPositions = new List<Vector3>();
            for (var i = 0; i < 8; i++)
            {
                float azimuth = angles[i];
                string dirname = mCameraDirections[i];
                Vector3 pos = getPosOnSphere(mDistance, new Degree(-azimuth), -altitude);
                mCameraPositions.Add(pos);
                Console.WriteLine("Determined camera pos: {0,2} is {1,5:F2},{2,5:F2},{3,5:F2}", dirname, pos.x, pos.y, pos.z);
            }
            /*
            mCameraPositions = new List<Vector3> {
                new Vector3(0, mDistance, mDistance),                   // Front / South
                new Vector3(-mPythDistance, mDistance, mPythDistance),  // Front-right / southwest
                new Vector3(-mDistance, mDistance, 0),                  // Right / west
                new Vector3(-mPythDistance, mDistance, -mPythDistance), // Back-right / northwest
                new Vector3(0, mDistance, -mDistance),                  // Back / north
                new Vector3(mPythDistance, mDistance, -mPythDistance),  // Back-left / northeast
                new Vector3(mDistance, mDistance, 0),                   // Left / east
                new Vector3(mPythDistance, mDistance, mPythDistance),   // Front-left / southeast
            };
            */



            //CompositorManager.Singleton.AddCompositor(vp, "EdgeDetectCompositor", 0);
            //CompositorManager.Singleton.AddCompositor(rtvp, "EdgeDetectCompositor", 0);
            //CompositorManager.Singleton.SetCompositorEnabled(vp, "EdgeDetectCompositor", true);
            //CompositorManager.Singleton.SetCompositorEnabled(rtvp, "EdgeDetectCompositor", true); 

            CompositorManager.Singleton.AddCompositor(vp, "Pixelate", 0);
            CompositorManager.Singleton.AddCompositor(mRTVP, "Pixelate", 0);
            CompositorManager.Singleton.SetCompositorEnabled(vp, "Pixelate", true);
            CompositorManager.Singleton.SetCompositorEnabled(mRTVP, "Pixelate", true);

            //CompositorManager.Singleton.AddCompositor(vp, "Normal", 0);
            //CompositorManager.Singleton.AddCompositor(rtvp, "Normal", 0);
            //CompositorManager.Singleton.SetCompositorEnabled(vp, "Normal", true);
            //CompositorManager.Singleton.SetCompositorEnabled(rtvp, "Normal", true);

            //CompositorManager.Singleton.AddCompositor(vp, "SMAA", 0);
            //CompositorManager.Singleton.AddCompositor(rtvp, "SMAA", 0);
            //CompositorManager.Singleton.SetCompositorEnabled(vp, "SMAA", true);
            //CompositorManager.Singleton.SetCompositorEnabled(rtvp, "SMAA", true);

            //CompositorManager.Singleton.AddCompositor(vp, "FXAA", 0);
            //CompositorManager.Singleton.AddCompositor(rtvp, "FXAA", 0);
            //CompositorManager.Singleton.SetCompositorEnabled(vp, "FXAA", true);
            //CompositorManager.Singleton.SetCompositorEnabled(rtvp, "FXAA", true);

            //Set up task scheduler
            imageTrimScheduler = new LimitedConcurrencyLevelTaskScheduler(3);
            imageTrimTaskFactory = new TaskFactory(imageTrimScheduler);
            return setup;
        }

        private void CreateTasks()
        {
            CreateItemTasks();
            CreateWearableTasks();
            CreateModelTasks();
        }

        private void CreateItemTasks()
        {
            foreach (var itemConfig in mItemConfigs.Values)
            {
                foreach (var playerModelConfig in mPlayerModelConfigs.Values)
                {
                    var task = new RenderTask
                                   {
                                       ItemConfig = itemConfig,
                                       PlayerModelConfig = playerModelConfig,
                                       TaskType = TaskType.Item
                                   };
                    renderTasks.Enqueue(task);
                }
            }
        }

        private void CreateWearableTasks()
        {
            foreach (var wearableConfig in mWearableConfigs.Values)
            {
                if (!mPlayerModelConfigs.ContainsKey(wearableConfig.ModelName))
                    continue;

                var task = new RenderTask
                {
                    WearableConfig = wearableConfig,
                    PlayerModelConfig = mPlayerModelConfigs[wearableConfig.ModelName],
                    TaskType = TaskType.Wearable
                };
                renderTasks.Enqueue(task);

            }
        }

        private void CreateModelTasks()
        {
            foreach (var playerModelConfig in mPlayerModelConfigs.Values)
            {
                var task = new RenderTask
                {
                    PlayerModelConfig = playerModelConfig,
                    TaskType = TaskType.BaseModel
                };
                renderTasks.Enqueue(task);

            }
        }

        private void ReadConfigs()
        {
            ReadPlayerModelConfigs();
            ReadWearableConfigs();
            ReadItemConfigs();
        }

        private void ReadPlayerModelConfigs()
        {
            var playerModelConfigFiles = GetPlayerModelConfigList();
            var deserializer = new XmlSerializer(typeof(PlayerModelConfig));
            foreach (var c in playerModelConfigFiles)
            {
                using (var xmlReader = XmlReader.Create(c))
                {
                    try
                    {
                        if (!deserializer.CanDeserialize(xmlReader))
                            throw new Exception("Invalid player model config: " + c);
                        var config = (PlayerModelConfig)deserializer.Deserialize(xmlReader);
                        mPlayerModelConfigs[config.EntityName] = config;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }

        private void ReadItemConfigs()
        {
            var itemConfigFiles = GetItemConfigList();
            var deserializer = new XmlSerializer(typeof(ItemConfig));
            foreach (var c in itemConfigFiles)
            {
                using (var xmlReader = XmlReader.Create(c))
                {
                    try
                    {
                        if (!deserializer.CanDeserialize(xmlReader))
                            throw new Exception("Invalid item config: " + c);
                        var config = (ItemConfig)deserializer.Deserialize(xmlReader);
                        mItemConfigs[config.EntityName] = config;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }

        private void ReadWearableConfigs()
        {
            var wearableConfigFiles = GetWearableConfigList();
            var deserializer = new XmlSerializer(typeof(WearableConfig));
            foreach (var c in wearableConfigFiles)
            {
                using (var xmlReader = XmlReader.Create(c))
                {
                    try
                    {
                        if (!deserializer.CanDeserialize(xmlReader))
                            throw new Exception("Invalid wearable config: " + c);
                        var config = (WearableConfig)deserializer.Deserialize(xmlReader);
                        mWearableConfigs[config.EntityName] = config;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }

        private IEnumerable<string> GetPlayerModelConfigList()
        {
            var currentDir = Environment.CurrentDirectory;
            var configsDir = currentDir + @"\Configs\PlayerModel";
            var files = Directory.GetFiles(configsDir, "*.xml", SearchOption.AllDirectories);
            return files;
        }

        private IEnumerable<string> GetItemConfigList()
        {
            var currentDir = Environment.CurrentDirectory;
            var configsDir = currentDir + @"\Configs\Item";
            var files = Directory.GetFiles(configsDir, "*.xml", SearchOption.AllDirectories);
            return files;
        }

        private IEnumerable<string> GetWearableConfigList()
        {
            var currentDir = Environment.CurrentDirectory;
            var configsDir = currentDir + @"\Configs\Wearable";
            var files = Directory.GetFiles(configsDir, "*.xml", SearchOption.AllDirectories);
            return files;
        }


        protected override void CreateScene()
        {
            base.CreateScene();
            mSceneMgr.AmbientLight = new ColourValue(1, 1, 1);
            mCamera.ProjectionType = ProjectionType.PT_ORTHOGRAPHIC;
            mCamera.LookAt(_sceneCenter);
            mCamera.OrthoWindowHeight = 50f;
            mCamera.OrthoWindowWidth = 50f;
        }

        protected void BeginNextTask()
        {
            //If we're transitioning from a previous task, lets tear that shit down
            if (currentRenderTask != null)
                TearDownPreviousTask();

            if (currentRenderTask == null && renderTasks.Count == 0)
            {
                mShutDown = true;
                return;
            }

            //Dequeue the next task
            currentRenderTask = renderTasks.Dequeue();

            _playerEntity = mSceneMgr.CreateEntity(currentRenderTask.PlayerModelConfig.EntityName, currentRenderTask.PlayerModelConfig.ModelName);

            _playerNode = mSceneMgr.RootSceneNode.CreateChildSceneNode("PlayerNode");
            _playerNode.AttachObject(_playerEntity);
            _playerNode._setDerivedPosition(_playerNode.Position + new Vector3(0, -0.5f * _playerEntity.BoundingRadius, 0));

            //Set up name of dat animation collection
            var animationCollectionName = currentRenderTask.PlayerModelConfig.EntityName;
            switch (currentRenderTask.TaskType)
            {
                case TaskType.Item:
                    animationCollectionName += "_" + currentRenderTask.ItemConfig.EntityName;
                    break;
                case TaskType.Wearable:
                    animationCollectionName += "_" + currentRenderTask.WearableConfig.EntityName;
                    break;
            }

            mAnimationCollection = new AnimationCollection { Name = animationCollectionName };
            mStateIterator = _playerEntity.AllAnimationStates.GetAnimationStateIterator();

            if (_playerEntity != null)
            {
                switch (currentRenderTask.TaskType)
                {
                    case TaskType.Item:
                        {
                            _currentItem = mSceneMgr.CreateEntity(currentRenderTask.ItemConfig.EntityName,
                                                                  currentRenderTask.ItemConfig.ModelName);
                            var ic = currentRenderTask.ItemConfig;
                            _playerEntity.AttachObjectToBone(currentRenderTask.PlayerModelConfig.RHandBoneName, _currentItem,
                                                             new Quaternion(ic.QuatW, ic.QuatX, ic.QuatY, ic.QuatZ),
                                                             new Vector3(ic.OffsetX, ic.OffsetY, ic.OffsetZ));
                            //Make player entity transparent so all we see is the item
                            _playerEntity.SetMaterialName("transparent_mask");
                        }
                        break;
                    case TaskType.Wearable:
                        _playerEntity.SetMaterialName(currentRenderTask.WearableConfig.Material);
                        break;
                }
            }


            //_ent.SetMaterialName("Normal");
            /*
      var sub = _ent.GetSubEntity(0);
      sub.SetCustomParameter(CUSTOM_SHININESS, new Vector4(10.0f, 0.0f, 0.0f, 0.0f));
      sub.SetCustomParameter(CUSTOM_DIFFUSE, new Vector4(1.0f, 1.0f, 0.7f, 1.0f));
      sub.SetCustomParameter(CUSTOM_SPECULAR, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));*/
            NextAnimation();
        }

        private void TearDownPreviousTask()
        {
            mSceneMgr.DestroyAllEntities();
            mSceneMgr.DestroyAllAnimations();
            _playerNode.RemoveAndDestroyAllChildren();
            mSceneMgr.DestroySceneNode(_playerNode);
            _playerEntity = null;
            _playerNode.Dispose();
            _playerNode = null;
            mAnimationCollection = null;
            mStateIterator = null;
            mRenderedAnims = new List<string>();
            mCurrentAnimState = null;
            mAnimTime = 0;
            mAnimMaxTime = 0;
            mFrameCount = 0;
            currentRenderTask = null;
        }

        protected void AddAnimTime(float time)
        {
            mCurrentAnimState.AddTime(time);
        }

        protected void DisableCurrentAnim()
        {
            mCurrentAnimState.Enabled = false;
        }

        protected bool MoveToNextAnimState()
        {
            mStateIterator.MoveNext();
            if (mRenderedAnims.Contains(mStateIterator.CurrentKey))
            {
                mCurrentAnimState = null;
                return false;
            }
            mCurrentAnimState = mStateIterator.Current;
            mCurrentAnimState.Enabled = true;

            mAnimationInfo = new AnimationInfo
                                {
                                    FPS = mAnimationFramesPerSecond,
                                    Frames = 0,
                                    Name = mCurrentAnimState.AnimationName
                                };
            mAnimationCollection.Animations.Add(mAnimationInfo);
            return true;
        }

        protected bool IsCurrentAnimEnded()
        {
            if (mAnimTime >= mCurrentAnimState.Length)
                return true;
            return false;
        }

        protected bool NextAnimation()
        {
            if (mCurrentAnimState != null)
            {
                AddAnimTime(1.0f);
                DisableCurrentAnim();
            }

            if (!MoveToNextAnimState())
            {
                return false;
            }
            else
            {
                mAnimMaxTime = mCurrentAnimState.Length;
                mAnimTime = 0;
                mFrameCount = 0;
                mRenderedAnims.Add(mStateIterator.CurrentKey);
                return true;
            }
        }

        protected bool NextMesh()
        {
            return true;
        }

        protected void WriteAnimationCollection()
        {
            var filename = "output\\anim_" + currentRenderTask.PlayerModelConfig.EntityName;
            if (currentRenderTask.TaskType == TaskType.Item)
                filename = filename + "_" + currentRenderTask.ItemConfig.EntityName;
            else if (currentRenderTask.TaskType == TaskType.Wearable)
                filename = filename + "_" + currentRenderTask.WearableConfig.EntityName;

            filename = filename + ".xml";

            var writer = new XmlSerializer(typeof(AnimationCollection));
            StreamWriter file = new StreamWriter(filename);
            writer.Serialize(file, mAnimationCollection);
            file.Close();
        }

        protected override void UpdateScene(FrameEvent evt)
        {
            base.UpdateScene(evt);
            //_currentAnimState.AddTime(e.TimeSinceLastFrame);
            if (mWindow.IsClosed)
                return;

            if (currentRenderTask == null && renderTasks.Count > 0)
                BeginNextTask();
            if (currentRenderTask == null)
            {
                mShutDown = true;
                return;
            }

            if (mCameraPosition > 7)
            {
                mFrameCount++;
                mAnimTime += (1 / (float)mAnimationFramesPerSecond);
                AddAnimTime((1 / (float)mAnimationFramesPerSecond));
                mAnimationInfo.Frames = mFrameCount;
                if (mAnimTime > mAnimMaxTime)
                {
                    NextAnimation();
                    if (mCurrentAnimState == null)
                    {
                        WriteAnimationCollection();
                        BeginNextTask();
                        mCameraPosition = 0;
                        return;
                    }
                }
                mCameraPosition = 0;
            }

            mCamera.Position = mCameraPositions[mCameraPosition];
            mCamera.LookAt(_sceneCenter);
            mWindow.Update();
            mRTT.Update();
            var filename = "output\\__pretrim_" + currentRenderTask.PlayerModelConfig.EntityName + "_";
            if (currentRenderTask.TaskType == TaskType.Item)
                filename += currentRenderTask.ItemConfig.EntityName + "_";
            else if (currentRenderTask.TaskType == TaskType.Wearable)
                filename += currentRenderTask.WearableConfig.EntityName + "_";
            filename += mCurrentAnimState.AnimationName.ToLowerInvariant() + "_" +
                        mCameraDirections[mCameraPosition] +
                        "_" +
                        mFrameCount + ".png";

            mRTT.WriteContentsToFile(filename);

            imageTrimTaskFactory.StartNew(TrimSprite, filename);

            mCameraPosition++;
        }

        public void TrimSprite(object fname)
        {
            string filename = fname as string;
            System.Drawing.Image image;
            using (var fileStream = new FileStream(filename, FileMode.Open))
            {
                image = System.Drawing.Image.FromStream(fileStream);
                fileStream.Close();
            }
            File.Delete(filename);

            filename = filename.Replace("__pretrim_", "");

            using (Bitmap bmp = new Bitmap(image))
            {
                var centerX = bmp.Width / 2;
                var centerY = bmp.Width / 2;
                var x = centerX - 1;
                var y = centerY - 1;
                //Loop quadrants.

                //Left side of image
                for (var i = 0; i < centerX; i++)
                {
                    //Top of image
                    for (var j = 0; j < centerY; j++)
                    {
                        var pix = bmp.GetPixel(i, j);
                        if (pix.A > 0)
                        {
                            if (i < x)
                                x = i;
                            if (j < y)
                                y = j;
                        }
                    }
                    //Bottom of image
                    for (var j = bmp.Height - 1; j > centerY; j--)
                    {
                        var pix = bmp.GetPixel(i, j);
                        if (pix.A > 0)
                        {
                            if (i < x)
                                x = i;
                            if (j > (bmp.Height - 1 - y))
                                y = (bmp.Height - 1 - j);
                        }
                    }
                }
                //Right side of image
                for (var i = bmp.Width - 1; i > centerY; i--)
                {                    //Top of image
                    for (var j = 0; j < centerY; j++)
                    {
                        var pix = bmp.GetPixel(i, j);
                        if (pix.A > 0)
                        {
                            if (i > (bmp.Width - 1 - x))
                                x = (bmp.Width - 1 - i);
                            if (j < y)
                                y = j;
                        }
                    }
                    //Bottom of image
                    for (var j = bmp.Height - 1; j > centerY; j--)
                    {
                        var pix = bmp.GetPixel(i, j);
                        if (pix.A > 0)
                        {
                            if (i > (bmp.Width - 1 - x))
                                x = (bmp.Width - 1 - i);
                            if (j > (bmp.Height - 1 - y))
                                y = (bmp.Height - 1 - j);
                        }
                    }
                }
                if (x > 0 || y > 0)
                {
                    var newX = x - 1;
                    if (newX < 0)
                        newX = 0;
                    var newY = y - 1;
                    if (newY < 0)
                        newY = 0;
                    var newWidth = bmp.Width - x * 2;
                    var newHeight = bmp.Height - y * 2;
                    WriteBitmap((string)filename,
                                bmp.Clone(new System.Drawing.Rectangle(newX, newY, newWidth, newHeight), bmp.PixelFormat));
                }
            }
        }

        public void WriteBitmap(string filename, Bitmap newBitmap)
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(dir, filename);
            newBitmap.Save(path, ImageFormat.Png);
        }
    }
}

// namespace MSpriteRenderer
