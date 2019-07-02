using System.Windows.Forms;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;


namespace Graphics
{
    public partial class GraphicsForm : MaterialSkin.Controls.MaterialForm
    {
        Renderer renderer = new Renderer();
        Thread MainLoopThread;
        public static Dictionary<string, List<float>> vertexDict = new Dictionary<string, List<float>>();
        public static Dictionary<string, List<float>> upTodateVertexDict = new Dictionary<string, List<float>>();
        //public static Dictionary<string, Dictionary<string, List<float>>> vertexKeyFramesDict = new Dictionary<string, Dictionary<string, List<float>>>();
        public static Dictionary<string, KeyFrame> vertexKeyFramesDict = new Dictionary<string, KeyFrame>();
        //count number of occurences for each mode
        public static Dictionary<string, int> modesDictCount = new Dictionary<string, int>();
        public static Dictionary<string, Tuple<int, int>> modesDict = new Dictionary<string, Tuple<int, int>>();
        public static List<ushort> vertexIndices = new List<ushort>();
        public static List<float> lightDataList;
        List<float> vertexList;
        public static string texturePath;
        public static bool applyTexture = false;
        public static bool applyLight = false;

        int vertexCounter = 0;

        int keyFrameCounter = 0;
        List<KeyFrame> keyFramesList = new List<KeyFrame>();
        public static int startFrame, endFrame;
        public static bool animationLoop = false;
        public static bool runAnimation = false;
        

        float xPos = float.MaxValue, yPos = float.MaxValue,
              zPos = float.MaxValue, rColor = float.MaxValue,
              gColor = float.MaxValue, bColor = float.MaxValue,
              udata = float.MaxValue, vdata = float.MaxValue,
              normalX = float.MaxValue, normalY = float.MaxValue,
              normalZ = float.MaxValue;

        float ambR = float.MaxValue, ambG = float.MaxValue,
              ambB = float.MaxValue, diffR = float.MaxValue,
              diffG = float.MaxValue, diffB = float.MaxValue,
              specR = float.MaxValue, specG = float.MaxValue,
              specB = float.MaxValue, lightPosX = float.MaxValue,
              lightPosY = float.MaxValue, lightPosZ = float.MaxValue,
              specExpo = float.MaxValue;

        float deltaTime;


        public GraphicsForm()
        {
            InitializeComponent();
            simpleOpenGlControl1.InitializeContexts();
            initialize();
            deltaTime = 0.005f;

            MainLoopThread = new Thread(MainLoop);
            MainLoopThread.Start();
        }

        float prevX, prevY;
        private void MoveCursor()
        {
            this.Cursor = new Cursor(Cursor.Current.Handle);
            Point p = PointToScreen(simpleOpenGlControl1.Location);
            Cursor.Position = new Point(simpleOpenGlControl1.Size.Width / 2 + p.X, simpleOpenGlControl1.Size.Height / 2 + p.Y);
            Cursor.Clip = new Rectangle(this.Location, this.Size);
            prevX = simpleOpenGlControl1.Location.X + simpleOpenGlControl1.Size.Width / 2;
            prevY = simpleOpenGlControl1.Location.Y + simpleOpenGlControl1.Size.Height / 2;
        }
        
        void initialize()
        {
            renderer.Initialize();
        }

        void MainLoop()
        {
            while (true)
            {
                renderer.Update();
                renderer.Draw();
                try
                {
                    simpleOpenGlControl1.Refresh();
                }
                catch { }
            }
        }

        private void GraphicsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            renderer.CleanUp();
            MainLoopThread.Abort();
        }

        private void simpleOpenGlControl1_Paint(object sender, PaintEventArgs e)
        {
            renderer.Draw();
        }

        string selectedKeyframe;
        private void keyFarmesAnimationListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            //show the vertices of the selected keyframe
            if (keyFarmesAnimationListBox.SelectedItem != null)
            {
                selectedKeyframe = keyFarmesAnimationListBox.SelectedItem.ToString();
                currentVerticesAnimationListBox.Items.Clear();

                foreach (string key in vertexKeyFramesDict[selectedKeyframe].verticesDict.Keys)
                    currentVerticesAnimationListBox.Items.Add(key);
            }
        }

        private void DeleteKeyFrameButton_Click(object sender, EventArgs e)
        {
            if (keyFarmesAnimationListBox.SelectedItem != null)
            {
                vertexKeyFramesDict.Remove(selectedKeyframe);
                MessageBox.Show(selectedKeyframe + " deleted");
                keyFarmesAnimationListBox.Items.Remove(selectedKeyframe);
                currentVerticesAnimationListBox.Items.Clear();
            }
            else
                MessageBox.Show("Please select a keyframe");
        }

        private void StopAnimationButton_Click(object sender, EventArgs e)
        {
            runAnimation = false;
            Renderer.setInterpolationId = false;
            //reset
            Renderer.interpolatedList.Clear();
            renderer.Draw();
        }

        private void UpdateKeyFrameButton_Click(object sender, EventArgs e)
        {
            selectedKeyframe = keyFarmesAnimationListBox.SelectedItem.ToString();
            string selectedVertex = currentVerticesAnimationListBox.SelectedItem.ToString();

            //vertexKeyFramesDict[selectedKeyframe][selectedVertex]
        }

        private void UpdateKeyFrameButton_Click_1(object sender, EventArgs e)
        {
            string selectedKeyFrame = keyFarmesAnimationListBox.SelectedItem.ToString();
            Dictionary<string, List<float>> Dict = new Dictionary<string, List<float>>(vertexKeyFramesDict[selectedKeyFrame].verticesDict);

            KeyFrame updatedKeyFrame = new KeyFrame(Dict, Convert.ToInt32(InterpolateFrametextBox.Text));

            vertexKeyFramesDict[selectedKeyFrame] = updatedKeyFrame;
        }

        private void PauseAnimationButton_Click(object sender, EventArgs e)
        {

        }


        string path, modelName;
        private void savePathButton_Click(object sender, EventArgs e)
        {
            
            FolderBrowserDialog folderDlg = new FolderBrowserDialog();
            folderDlg.ShowNewFolderButton = true;
            DialogResult result = folderDlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                pathTextBox.Text = folderDlg.SelectedPath;
            }
            path = pathTextBox.Text;
        }

        private void exportButton_Click(object sender, EventArgs e)
        {
            
            modelName = modelNameTextBox.Text;
            path = Path.Combine(path, modelName);
            saveDataToFile(path);
            MessageBox.Show("Done");
        }

        private void importButton_Click(object sender, EventArgs e)
        {
            
            //import vertex data
            //path = path.Replace("\\", "\\\\");
            modelName = modelNameTextBox.Text;
            path = Path.Combine(path, modelName);
            string[] data = File.ReadAllLines(path);
            string[] finalData = new string[data.Length];
            int j = 0;
            for (int i = 0; i < data.Length / 2; i++)
            {
                finalData[i] = data[j];
                j += 2;
            }
            importData(finalData);
            putDatainGUI();
            MessageBox.Show("Done");
            
        }
        public void putDatainGUI()
        {
            
            foreach (KeyValuePair<string, List<float>> entry in vertexDict) //add vertices name to current vertices list
            {
                currentVerticesListBox.Items.Add(entry.Key.ToString());
            }
            // add indices to indices tesxt box
            indicesTextBox.Text = "";
            for (int i = 0; i < vertexIndices.Count; i++)
            {
                indicesTextBox.Text += vertexIndices[i].ToString() + ",";
            }
            // add modes to current mode list
            foreach (KeyValuePair<string, Tuple<int, int>> entry in modesDict) // add modes
            {
                modesListBox.Items.Add(entry.Key.ToString());
            }
            // add light // hal feh haga tt7at??
            ambRtextBox.Text = lightDataList[0].ToString();
            ambGtextBox.Text = lightDataList[1].ToString();
            ambBtextBox.Text = lightDataList[2].ToString();
            diffRtextBox.Text = lightDataList[3].ToString();
            diffGtextBox.Text = lightDataList[4].ToString();
            diffBtextBox.Text = lightDataList[5].ToString();

            specRtextBox.Text = lightDataList[6].ToString();
            specGtextBox.Text = lightDataList[7].ToString();
            specBtextBox.Text = lightDataList[8].ToString();

            lightPosXtextBox.Text =lightDataList[9].ToString();
            lightposYtextBox.Text =lightDataList[10].ToString();
            lightPosZtextBox.Text =lightDataList[11].ToString();

            specExpotextBox.Text = lightDataList[12].ToString();

            // add texture path ,, hal feh haga tthat brdo??
            texturePathTextBox.Text = texturePath;
            // add key frames
            foreach (KeyValuePair<string, KeyFrame> entry in vertexKeyFramesDict)  // add el key frames
            {
                keyFarmesAnimationListBox.Items.Add(entry.Key.ToString());
                foreach (KeyValuePair<string, List<float>> d in entry.Value.verticesDict)
                {
                    currentVerticesAnimationListBox.Items.Add(d.Key.ToString());
                }
            }
            
        }
        public void importData(string[] data)
        {
            
            int verticesCount = Convert.ToInt32(data[0]);
            for (int i = 0; i < verticesCount; i++)
            {
                string[] vertexData = data[i + 1].Split(' ');
                List<float> vertexList = new List<float>();

                vertexList.Add(float.Parse(vertexData[1]));
                vertexList.Add(float.Parse(vertexData[2]));
                vertexList.Add(float.Parse(vertexData[3]));
                vertexList.Add(float.Parse(vertexData[4]));
                vertexList.Add(float.Parse(vertexData[5]));
                vertexList.Add(float.Parse(vertexData[6]));
                vertexList.Add(float.Parse(vertexData[7]));
                vertexList.Add(float.Parse(vertexData[8]));
                vertexList.Add(float.Parse(vertexData[9]));
                vertexList.Add(float.Parse(vertexData[10]));
                vertexList.Add(float.Parse(vertexData[11]));

                vertexDict[vertexData[0]] = vertexList;
                
            }

            // import indices
            int indicesCount = Convert.ToInt32(data[verticesCount + 1]);
            string[] indices = data[verticesCount + 2].Split(' ');
            for (int i = 0; i < indicesCount; i++)
            {
                vertexIndices.Add(Convert.ToUInt16(indices[i]));
            }

            // import mode
            int modesCount = Convert.ToInt32(data[verticesCount + 3]);
            for (int i = 0; i < modesCount; i++)
            {
                string[] modess = data[modesCount + verticesCount + 3 + i].Split(' ');
                string modeName = modess[0] + " " + modess[1];
                int startIndex = Convert.ToInt32(modess[2]);
                int countIndices = Convert.ToInt32(modess[3]);
                Tuple<int, int> t = new Tuple<int, int>(startIndex, countIndices);
                modesDict[modeName] = t;
            }
            // import light
            string[] lightData = data[verticesCount + 3 + modesCount + 1].Split(' ');
            lightDataList = new List<float>();
            lightDataList.Add(float.Parse(lightData[0]));
            lightDataList.Add(float.Parse(lightData[1]));
            lightDataList.Add(float.Parse(lightData[2]));
            lightDataList.Add(float.Parse(lightData[3]));
            lightDataList.Add(float.Parse(lightData[4]));
            lightDataList.Add(float.Parse(lightData[5]));
            lightDataList.Add(float.Parse(lightData[6]));
            lightDataList.Add(float.Parse(lightData[7]));
            lightDataList.Add(float.Parse(lightData[8]));
            lightDataList.Add(float.Parse(lightData[9]));
            lightDataList.Add(float.Parse(lightData[10]));
            lightDataList.Add(float.Parse(lightData[11]));
            lightDataList.Add(float.Parse(lightData[12]));

            // import texture path
            texturePath = data[verticesCount + 3 + modesCount + 1 + 1];

            // import key frames
            int keyFrameCount = Convert.ToInt32(data[verticesCount + 3 + modesCount + 1 + 1 + 1]);
            List<float> vertexListt;
            for (int i = 0; i < keyFrameCount; i++)
            {
                string[] keyFrameData = data[verticesCount + 3 + modesCount + 1 + 1 + 1 + i + 1].Split(' ');
                string keyFrameName = keyFrameData[0] + " " + keyFrameData[1];
                Dictionary<string, List<float>> newDict = new Dictionary<string, List<float>>();

                for (int j = 0; j < verticesCount; j++)
                {
                    vertexListt = new List<float>();
                    vertexListt.Add(float.Parse(keyFrameData[3 + (12 * j)]));
                    vertexListt.Add(float.Parse(keyFrameData[4 + (12 * j)]));
                    vertexListt.Add(float.Parse(keyFrameData[5 + (12 * j)]));
                    vertexListt.Add(float.Parse(keyFrameData[6 + (12 * j)]));
                    vertexListt.Add(float.Parse(keyFrameData[7 + (12 * j)]));
                    vertexListt.Add(float.Parse(keyFrameData[8 + (12 * j)]));
                    vertexListt.Add(float.Parse(keyFrameData[9 + (12 * j)]));
                    vertexListt.Add(float.Parse(keyFrameData[10 + (12 * j)]));
                    vertexListt.Add(float.Parse(keyFrameData[11 + (12 * j)]));
                    vertexListt.Add(float.Parse(keyFrameData[12 + (12 * j)]));
                    vertexListt.Add(float.Parse(keyFrameData[13 + (12 * j)]));
                    newDict[keyFrameData[2 + (12 * j)]] = vertexListt;
                }
                KeyFrame updatedKeyFrame = new KeyFrame(newDict, Convert.ToInt32(keyFrameData[keyFrameData.Length - 2]));
                vertexKeyFramesDict[keyFrameName] = updatedKeyFrame;

            }
            
        }
        public void saveDataToFile(string path)
        {
            
            string[] ss = new string[1];
            ss[0] = "\n";
            //StreamWriter streamWriter = new StreamWriter("myfile.txt" , true);
            File.AppendAllText(path, vertexDict.Count.ToString());
            File.AppendAllLines(path, ss);

            foreach (KeyValuePair<string, List<float>> entry in vertexDict) //add vertex data ,, tab 1
            {
                File.AppendAllText(path, entry.Key + " ");
                for (int i = 0; i < entry.Value.Count; i++)
                {
                    File.AppendAllText(path, entry.Value[i].ToString() + " ");
                }
                File.AppendAllLines(path, ss);
            }
            File.AppendAllText(path, vertexDict.Count.ToString());
            File.AppendAllLines(path, ss);
            for (int i = 0; i < vertexIndices.Count; i++)
            {
                File.AppendAllText(path, vertexIndices[i].ToString() + " ");
            }
            File.AppendAllLines(path, ss);
            File.AppendAllText(path, modesDict.Count.ToString());
            File.AppendAllLines(path, ss);
            foreach (KeyValuePair<string, Tuple<int, int>> entry in modesDict) // add modes
            {
                File.AppendAllText(path, entry.Key + " ");
                File.AppendAllText(path, entry.Value.Item1.ToString() + " ");
                File.AppendAllText(path, entry.Value.Item2.ToString() + " ");
                File.AppendAllLines(path, ss);
            }
            for (int i = 0; i < lightDataList.Count; i++)   // add light
            {
                File.AppendAllText(path, lightDataList[i].ToString() + " ");
            }
            File.AppendAllLines(path, ss);
            File.AppendAllText(path, texturePath + " ");
            File.AppendAllLines(path, ss);
            File.AppendAllText(path, vertexKeyFramesDict.Count.ToString());
            File.AppendAllLines(path, ss);
            foreach (KeyValuePair<string, KeyFrame> entry in vertexKeyFramesDict)  // add el key frames
            {
                File.AppendAllText(path, entry.Key.ToString() + " ");//esm el keyframe

                foreach (KeyValuePair<string, List<float>> d in entry.Value.verticesDict)
                {
                    File.AppendAllText(path, d.Key.ToString() + " ");//esm el vertex
                    for (int i = 0; i < d.Value.Count; i++)
                    {
                        File.AppendAllText(path, d.Value[i].ToString() + " ");//el values bta3et kol vertex
                    }
                }
                File.AppendAllText(path, entry.Value.numOfInterpolatedFrames.ToString() + " ");// num of interpolation
                File.AppendAllLines(path, ss);
            }
            
        }

        private void animationLoopcheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (animationLoopcheckBox.Checked)
                animationLoop = true;
            else
                animationLoop = false;
        }

        private void RunAnimationButton_Click(object sender, EventArgs e)
        {
            runAnimation = true;
            Renderer.setInterpolationId = true;
            startFrame = Convert.ToInt32(startFrametextBox.Text);
            endFrame = Convert.ToInt32(EndFrametextBox.Text);
            renderer.Update();
            renderer.Draw();
        }

        private void simpleOpenGlControl1_MouseMove(object sender, MouseEventArgs e)
        {
            float speed1 = 0.05f;
            float delta = e.X - prevX;
            if (delta > 2)
                renderer.cam.Yaw(-speed1);
            else if (delta < -2)
                renderer.cam.Yaw(speed1);

            delta = e.Y - prevY;
            if (delta > 2)
                renderer.cam.Pitch(-speed1);
            else if (delta < -2)
                renderer.cam.Pitch(speed1);

           //MoveCursor();
            
        }

        private void simpleOpenGlControl1_KeyPress(object sender, KeyPressEventArgs e)
        {
         
            float speed = 0.3f;
            if (e.KeyChar == 'a')
                renderer.cam.Strafe(-speed);
            if (e.KeyChar == 'd')
                renderer.cam.Strafe(speed);
            if (e.KeyChar == 's')
                renderer.cam.Walk(-speed);
            if (e.KeyChar == 'w')
                renderer.cam.Walk(speed);
            if (e.KeyChar == 'z')
                renderer.cam.Fly(-speed);
            if (e.KeyChar == 'c')
                renderer.cam.Fly(speed);
        }

        //add new keyframe
        private void materialFlatButton2_Click(object sender, EventArgs e)
        {
            keyFrameCounter++;
            string name = "Keyframe " + keyFrameCounter.ToString();

            //set the values of the new keyframe as the last one
            Dictionary<string, List<float>> newDict = new Dictionary<string, List<float>>(upTodateVertexDict);
            KeyFrame updatedKeyFrame = new KeyFrame(newDict, Convert.ToInt32(InterpolateFrametextBox.Text));
            vertexKeyFramesDict[name] = updatedKeyFrame;

            keyFarmesAnimationListBox.Items.Add(name);
        }

        

        //ya rab ostor
        //update vertex button
        private void updateButtonAnimation_Click(object sender, EventArgs e)
        {
            string selectedVertex = currentVerticesAnimationListBox.SelectedItem.ToString();
            string selectedKeyFrame = keyFarmesAnimationListBox.SelectedItem.ToString();
           // Dictionary<string, List<float>> keyFrameData = new Dictionary<string, List<float>>();

            #region updating
            if (xPosAnimationTextBox.Text != "")
                xPos = (float)Convert.ToDouble(xPosAnimationTextBox.Text);

            if (yPosAnimationTextBox.Text != "")
                 yPos = (float)Convert.ToDouble(yPosAnimationTextBox.Text);

            if (zPosAnimationTextBox.Text != "")
                zPos = (float)Convert.ToDouble(zPosAnimationTextBox.Text);

            if (rColorAnimationTextBox.Text != "")
                rColor = (float)Convert.ToDouble(rColorAnimationTextBox.Text);

            if (gColorAnimationTextBox.Text != "")
                gColor = (float)Convert.ToDouble(gColorAnimationTextBox.Text);

            if (bColorAnimationTextBox.Text != "")
                bColor = (float)Convert.ToDouble(bColorAnimationTextBox.Text);

            if (normalXAnimationTextBox.Text != "")
                normalX = (float)Convert.ToDouble(normalXAnimationTextBox.Text);

            if (normalYAnimationTextBox.Text != "")
                normalY = (float)Convert.ToDouble(normalYAnimationTextBox.Text);

            if (normalZAnimationTextBox.Text != "")
                normalZ = (float)Convert.ToDouble(normalZAnimationTextBox.Text);

            if (UdataAnimationTextBox.Text != "")
                udata = (float)Convert.ToDouble(UdataAnimationTextBox.Text);

            if (VdataAnimationTextBox.Text != "")
                vdata = (float)Convert.ToDouble(VdataAnimationTextBox.Text);

            List<float> vertexList = new List<float>();
            vertexList.Add(xPos);
            vertexList.Add(yPos);
            vertexList.Add(zPos);
            vertexList.Add(rColor);
            vertexList.Add(gColor);
            vertexList.Add(bColor);
            vertexList.Add(udata);
            vertexList.Add(vdata);
            vertexList.Add(normalX);
            vertexList.Add(normalY);
            vertexList.Add(normalZ);
            #endregion

            //3shan a7otha ll keyframe ely ba3do
            upTodateVertexDict[selectedVertex] = vertexList;
            //hashof law h3ml keda 3shan mayb2ash bt referece w kolohom byboso 3la nfs el dic
            Dictionary<string, List<float>> newDict = new Dictionary<string, List<float>>(upTodateVertexDict);
            
             KeyFrame updatedKeyFrame = new KeyFrame(newDict, Convert.ToInt32(InterpolateFrametextBox.Text));

             vertexKeyFramesDict[selectedKeyFrame] = updatedKeyFrame;

            //// vertexDict[selectedVertex] = vertexList;
            MessageBox.Show("Keyframe updated");
        }

        //tab load
        private void animationKeyframeTab_Enter(object sender, EventArgs e)
        {
            keyFarmesAnimationListBox.Items.Clear();
            //  vertexKeyFramesDict["Key Frame 0"] = vertexDict;
            KeyFrame startKeyFrame = new KeyFrame(vertexDict, 0);
            vertexKeyFramesDict["Keyframe 0"] = startKeyFrame;
            
            keyFarmesAnimationListBox.Items.Add("Keyframe 0");

            currentVerticesAnimationListBox.Items.Clear();

            foreach (string key in vertexDict.Keys)
                currentVerticesAnimationListBox.Items.Add(key);
        }

        private void currentVerticesAnimationListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedVertex = currentVerticesAnimationListBox.SelectedItem.ToString();
            string selectedKeyFrame = keyFarmesAnimationListBox.SelectedItem.ToString();
            List<float> vertexList = vertexKeyFramesDict[selectedKeyFrame].verticesDict[selectedVertex];
            //List<float> vertexList = upTodateVertexDict[selectedVertex];

            //to show data of selected vertex
            xPosAnimationTextBox.Text = vertexList[0].ToString();
            yPosAnimationTextBox.Text = vertexList[1].ToString();
            zPosAnimationTextBox.Text = vertexList[2].ToString();

            rColorAnimationTextBox.Text = vertexList[3].ToString();
            gColorAnimationTextBox.Text = vertexList[4].ToString();
            bColorAnimationTextBox.Text = vertexList[5].ToString();

            UdataAnimationTextBox.Text = vertexList[6].ToString();
            VdataAnimationTextBox.Text = vertexList[7].ToString();

            normalXAnimationTextBox.Text = vertexList[8].ToString();
            normalYAnimationTextBox.Text = vertexList[9].ToString();
            normalZAnimationTextBox.Text = vertexList[10].ToString();
        
        }
    
        private void updateModeButton_Click(object sender, EventArgs e)
        {
            //law 3aiz y8air esm el mode msh bs el start w el count
            string newMode = "";
            bool changeName = false;
            if (primitivesComboBox.SelectedIndex > -1)
            {
                newMode = primitivesComboBox.SelectedItem.ToString();
                changeName = true;
            }
            string selectedModeToUpdate = modesListBox.SelectedItem.ToString();
            Tuple<int, int> newData = new Tuple<int, int>(Convert.ToInt32(startIndexTextBox.Text), Convert.ToInt32(countIndicesTextBox.Text));

            foreach (string s in modesDict.Keys)
            {
                if(s == selectedModeToUpdate && !changeName)
                {
                    modesDict[s] = newData;
                    break;
                }
                else if(s == selectedModeToUpdate && changeName)
                {
                    //decreament mode's count in the dictionary
                    string[] mode = s.Split(' ');//get the mode name without its id
                    modesDictCount[mode[0]]--;
                    modesDict.Remove(s);
                    modesDictCount[newMode]++;
                    newMode += " " + modesDictCount[newMode];
                    modesDict[newMode] = newData;
                    modesListBox.Items[modesListBox.SelectedIndex] = newMode;
                    break;
                }
            }
        }

        private void modesListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedMode = "";

            if (modesListBox.SelectedItem!= null)
                selectedMode = modesListBox.SelectedItem.ToString();
            foreach (string s in modesDict.Keys)
            {
                if (s == selectedMode)
                {
                    primitivesComboBox.Text = s;
                    startIndexTextBox.Text = modesDict[s].Item1.ToString();
                    countIndicesTextBox.Text = modesDict[s].Item2.ToString();

                    break;
                }
            }
        }

        private void deleteModeButton_Click(object sender, EventArgs e)
        {
            string selectedModeToDelete = modesListBox.SelectedItem.ToString();
            foreach (string s in modesDict.Keys)
            {
                if (s == selectedModeToDelete)
                {
                    modesDict.Remove(s);
                    break;
                }
            }
            modesListBox.Items.Remove(modesListBox.SelectedItem.ToString());
            primitivesComboBox.Text = "";
            startIndexTextBox.Text = "";
            countIndicesTextBox.Text = "";
        }

        private void DrawButton_Click(object sender, EventArgs e)
        {
            renderer.updatebuffers();
        }

        private void browseImageButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) 
            {
                texturePath = fileDialog.FileName;
                texturePath = texturePath.Replace("\\", "\\\\");
                texturePathTextBox.Text = texturePath;
                pictureBox1.Image = Image.FromFile(texturePath);
                applyTexture = true;
            }
            renderer.setTexture();
            renderer.Draw();
        }

        private void TakeLightData()
        {
            lightDataList = new List<float>();

            if (ambRtextBox.Text != "")
                ambR = (float)Convert.ToDouble(ambRtextBox.Text);
            if (ambGtextBox.Text != "")
                ambG = (float)Convert.ToDouble(ambGtextBox.Text);
            if (ambBtextBox.Text != "")
                ambB = (float)Convert.ToDouble(ambBtextBox.Text);

            if (diffRtextBox.Text != "")
                diffR = (float)Convert.ToDouble(diffRtextBox.Text);
            if (diffGtextBox.Text != "")
                diffG = (float)Convert.ToDouble(diffGtextBox.Text);
            if (diffBtextBox.Text != "")
                diffB = (float)Convert.ToDouble(diffBtextBox.Text);

            if (specRtextBox.Text != "")
                specR = (float)Convert.ToDouble(specRtextBox.Text);
            if (specGtextBox.Text != "")
                specG = (float)Convert.ToDouble(specGtextBox.Text);
            if (specBtextBox.Text != "")
                specB = (float)Convert.ToDouble(specBtextBox.Text);

            if (lightPosXtextBox.Text != "")
                lightPosX = (float)Convert.ToDouble(lightPosXtextBox.Text);
            if (lightposYtextBox.Text != "")
                lightPosY = (float)Convert.ToDouble(lightposYtextBox.Text);
            if (lightPosZtextBox.Text != "")
                lightPosZ = (float)Convert.ToDouble(lightPosZtextBox.Text);

            if (specExpotextBox.Text != "")
                specExpo = (float)Convert.ToDouble(specExpotextBox.Text);

            lightDataList.Add(ambR);
            lightDataList.Add(ambG);
            lightDataList.Add(ambB);
            lightDataList.Add(diffR);
            lightDataList.Add(diffG);
            lightDataList.Add(diffB);
            lightDataList.Add(specR);
            lightDataList.Add(specG);
            lightDataList.Add(specB);
            lightDataList.Add(lightPosX);
            lightDataList.Add(lightPosY);
            lightDataList.Add(lightPosZ);
            lightDataList.Add(specExpo);
        }

        private void setLightbutton_Click(object sender, EventArgs e)
        {
            applyLight = true;
            TakeLightData();
            renderer.FillLightDataList();
        }

        private void disableLightbutton_Click(object sender, EventArgs e)
        {
            applyLight = false;
            lightDataList.Clear();
            renderer.Draw();
        }

        private void GraphicsForm_Load(object sender, EventArgs e)
        {
            modesDictCount["GL_LINES"] = -1;
            modesDictCount["GL_LINE_STRIP"] = -1;
            modesDictCount["GL_TRIANGLES"] = -1;
            modesDictCount["GL_LINE_LOOP"] = -1;
            modesDictCount["GL_POINTS"] = -1;
            modesDictCount["GL_TRIANGLE_STRIP"] = -1;
            modesDictCount["GL_TRIANGLE_FAN"] = -1;

            //foreach (KeyFrame kf in vertexKeyFramesDict.Values)
            //    MessageBox.Show((kf.verticesDict.Values).ToString());
        }

        private void addModeButton_Click(object sender, EventArgs e)
        {
            string selectedMode = primitivesComboBox.SelectedItem.ToString();
            int startIndex = Convert.ToInt32(startIndexTextBox.Text);
            int countIndices = Convert.ToInt32(countIndicesTextBox.Text);

            modesDictCount[selectedMode]++;
            selectedMode +=  " " + modesDictCount[selectedMode].ToString();
            Tuple<int, int> t = new Tuple<int, int>(startIndex, countIndices);
            modesDict[selectedMode] = t;
            modesListBox.Items.Add(selectedMode);
        }

        private void updateIndicesButton_Click(object sender, EventArgs e)
        {
            vertexIndices.Clear();
            string indices = indicesTextBox.Text;
            string[] indicesArray = indices.Split(',');

            for (int i = 0; i < indicesArray.Length; i++)
                vertexIndices.Add(Convert.ToUInt16(indicesArray[i]));
            MessageBox.Show("Indicies Updated");
        }

        private void deleteVertexButton_Click(object sender, EventArgs e)
        {
            string selectedVertex = currentVerticesListBox.SelectedItem.ToString();

            vertexDict.Remove(selectedVertex);
            currentVerticesListBox.Items.Remove(selectedVertex);

            xPosTextBox.Text = "";
            yPosTextBox.Text = "";
            zPosTextBox.Text = "";
            gTextBox.Text = "";
            bTextBox.Text = "";
            UTextBox.Text = "";
            rTextBox.Text = "";
            VTextBox.Text = "";
            normalXTextBox.Text = "";
            normalYTextBox.Text = "";
            normalZTextBox.Text = "";

            upTodateVertexDict = new Dictionary<string, List<float>>(vertexDict);
            MessageBox.Show("Vertex Deleted");
        }

        private void addVertexButton_Click(object sender, System.EventArgs e)
        {
            if (xPosTextBox.Text != "")
                xPos = (float)Convert.ToDouble(xPosTextBox.Text);
            if (yPosTextBox.Text != "")
                yPos = (float)Convert.ToDouble(yPosTextBox.Text);
            if (zPosTextBox.Text != "")
                zPos = (float)Convert.ToDouble(zPosTextBox.Text);
            if (rTextBox.Text != "")
                rColor = (float)Convert.ToDouble(rTextBox.Text);
            if (gTextBox.Text != "")
                gColor = (float)Convert.ToDouble(gTextBox.Text);
            if (bTextBox.Text != "")
                bColor = (float)Convert.ToDouble(bTextBox.Text);
            if (normalXTextBox.Text != "")
                normalX = (float)Convert.ToDouble(normalXTextBox.Text);
            if (normalYTextBox.Text != "")
                normalY = (float)Convert.ToDouble(normalYTextBox.Text);
            if (normalZTextBox.Text != "")
                normalZ = (float)Convert.ToDouble(normalZTextBox.Text);
            if (UTextBox.Text != "")
                udata = (float)Convert.ToDouble(UTextBox.Text);
            if (VTextBox.Text != "")
                vdata = (float)Convert.ToDouble(VTextBox.Text);

            string vertexName = "V";
            vertexName += vertexCounter.ToString();
            vertexCounter++;
            //List<float> vertexList = new List<float>();
            vertexList = new List<float>();

            vertexList.Add(xPos);
            vertexList.Add(yPos);
            vertexList.Add(zPos);
            vertexList.Add(rColor);
            vertexList.Add(gColor);
            vertexList.Add(bColor);
            vertexList.Add(udata);
            vertexList.Add(vdata);
            vertexList.Add(normalX);
            vertexList.Add(normalY);
            vertexList.Add(normalZ);

            vertexDict[vertexName] = vertexList;

            upTodateVertexDict = new Dictionary<string, List<float>>(vertexDict);
            currentVerticesListBox.Items.Add(vertexName);
            currentVerticesAnimationListBox.Items.Add(vertexName);
        }

        private void currentVerticesListBox_SelectedIndexChanged(object sender, EventArgs e)
        {

            string selectedVertex = "";
            if (currentVerticesListBox.SelectedItem != null)
            {
                selectedVertex = currentVerticesListBox.SelectedItem.ToString();
                List<float> vertexList = vertexDict[selectedVertex];

                //to show data of selected vertex
                xPosTextBox.Text = vertexList[0].ToString();
                yPosTextBox.Text = vertexList[1].ToString();
                zPosTextBox.Text = vertexList[2].ToString();

                rTextBox.Text = vertexList[3].ToString();
                gTextBox.Text = vertexList[4].ToString();
                bTextBox.Text = vertexList[5].ToString();

                UTextBox.Text = vertexList[6].ToString();
                VTextBox.Text = vertexList[7].ToString();

                normalXTextBox.Text = vertexList[8].ToString();
                normalYTextBox.Text = vertexList[9].ToString();
                normalZTextBox.Text = vertexList[10].ToString();
            }
            renderer.updatebuffers();
            Renderer.selectedVertex = true;
            Renderer.selectedVertexIndex = currentVerticesListBox.SelectedIndex;
        }

        private void updateVertexButton_Click(object sender, EventArgs e)
        {
            string selectedVertex = currentVerticesListBox.SelectedItem.ToString();
            if (xPosTextBox.Text != "")
                xPos = (float)Convert.ToDouble(xPosTextBox.Text);
            if (yPosTextBox.Text != "")
                yPos = (float)Convert.ToDouble(yPosTextBox.Text);
            if (zPosTextBox.Text != "")
                zPos = (float)Convert.ToDouble(zPosTextBox.Text);
            if (rTextBox.Text != "")
                rColor = (float)Convert.ToDouble(rTextBox.Text);
            if (gTextBox.Text != "")
                gColor = (float)Convert.ToDouble(gTextBox.Text);
            if (bTextBox.Text != "")
                bColor = (float)Convert.ToDouble(bTextBox.Text);
            if (normalXTextBox.Text != "")
                normalX = (float)Convert.ToDouble(normalXTextBox.Text);
            if (normalYTextBox.Text != "")
                normalY = (float)Convert.ToDouble(normalYTextBox.Text);
            if (normalZTextBox.Text != "")
                normalZ = (float)Convert.ToDouble(normalZTextBox.Text);
            if (UTextBox.Text != "")
                udata = (float)Convert.ToDouble(UTextBox.Text);
            if (VTextBox.Text != "")
                vdata = (float)Convert.ToDouble(VTextBox.Text);
            
            List<float> vertexList = new List<float>();
            vertexList.Add(xPos);
            vertexList.Add(yPos);
            vertexList.Add(zPos);
            vertexList.Add(rColor);
            vertexList.Add(gColor);
            vertexList.Add(bColor);
            vertexList.Add(udata);
            vertexList.Add(vdata);
            vertexList.Add(normalX);
            vertexList.Add(normalY);
            vertexList.Add(normalZ);

            vertexDict[selectedVertex] = vertexList;
            upTodateVertexDict = new Dictionary<string, List<float>>(vertexDict);
            MessageBox.Show("Vertex updated");
        }

        private void animationKeyframeTab_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {

        }

    }
}
