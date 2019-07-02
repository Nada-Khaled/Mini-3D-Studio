//stable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Tao.OpenGl;
using System.IO;
//include GLM library
using GlmNet;

namespace Graphics
{
    class Renderer
    {
        Shader sh;
        uint planeBufferID;

        //3D Drawing
        mat4 ModelMatrix;
        mat4 ViewMatrix;
        mat4 ProjectionMatrix;

        public static bool selectedVertex = false;
        public static int selectedVertexIndex;
        public List<float> verts = new List<float>();

        public List<ushort> indicies = new List<ushort>();
        uint vertexID, indiciesBufferID;

        Texture texture;
        int checkTextureID, checkLightID;

        int AmbientLightID, DiffuseLightID, SpecularLightID;
        int LightPosID;
        int EyePositionID;
        int DataID;

        public Camera cam;

        int transID, viewID, projID;

        //Keyframes
        float speed = 0.0001f;
        float time = 0;
        public static List<float> interpolatedList = new List<float>();
        List<float> tempKeyFrameList;
        List<Tuple<int, List<float>>> mainKeyFrameList = new List<Tuple<int, List<float>>>();
        int initialzeList = 1;
        public static bool setInterpolationId = false;

        public void setTexture()
        {
            texture = new Texture(GraphicsForm.texturePath, 1);
            texture.Bind();
        }

        public void updatebuffers()
        {
            verts = new List<float>();
            indicies = new List<ushort>();

            foreach (List<float> lst in GraphicsForm.vertexDict.Values)//
                for (int i = 0; i < lst.Count; i++)
                    verts.Add(lst[i]);

            foreach (ushort indx in GraphicsForm.vertexIndices)
                indicies.Add(indx);

            vertexID = GPU.GenerateBuffer(verts.ToArray());
            indiciesBufferID = GPU.GenerateElementBuffer(indicies.ToArray());

            
        }
        public void FillLightDataList()//3shan el list mabtkonsh etmlt lesa
        {
            if (GraphicsForm.lightDataList.Count != 0)
            {
                vec3 ambientLight = new vec3(GraphicsForm.lightDataList[0], GraphicsForm.lightDataList[1], GraphicsForm.lightDataList[2]);
                Gl.glUniform3fv(AmbientLightID, 1, ambientLight.to_array());

                vec3 diffuseLight = new vec3(GraphicsForm.lightDataList[3], GraphicsForm.lightDataList[4], GraphicsForm.lightDataList[5]);
                Gl.glUniform3fv(DiffuseLightID, 1, diffuseLight.to_array());

                vec3 specularLight = new vec3(GraphicsForm.lightDataList[6], GraphicsForm.lightDataList[7], GraphicsForm.lightDataList[8]);
                Gl.glUniform3fv(SpecularLightID, 1, specularLight.to_array());

                vec3 LightPosition = new vec3(GraphicsForm.lightDataList[9], GraphicsForm.lightDataList[10], GraphicsForm.lightDataList[11]);
                Gl.glUniform3fv(LightPosID, 1, LightPosition.to_array());

                //1st is attenuation fixed 2nd is specularExponent
                vec2 data = new vec2(50, GraphicsForm.lightDataList[12]);
                Gl.glUniform2fv(DataID, 1, data.to_array());
            }
        }

        public void Initialize()
        {
            string projectPath = Directory.GetParent(Environment.CurrentDirectory).Parent.FullName;
            sh = new Shader(projectPath + "\\Shaders\\SimpleVertexShader.vertexshader", projectPath + "\\Shaders\\SimpleFragmentShader.fragmentshader");
            Gl.glClearColor(0.6f, 0.6f, 0.6f, 1);

            float[] backGroundVertices ={

                //plane
                -2.0f,0.0f,2.0f,
                1.0f,1.0f,1.0f,     //color w
                -2.0f,0.0f,-2.0f,
                1.0f,1.0f,1.0f,     //color w
                2.0f,0.0f,-2.0f,
                1.0f,1.0f,1.0f,     //color w
                2.0f,0.0f,2.0f,
                1.0f,1.0f,1.0f,     //color w

		        //Axis
		        //x
		        -2.0f, 0.0f, 0.0f,
                1.0f, 0.0f, 0.0f, //R
		        2.0f, 0.0f, 0.0f,
                1.0f, 0.0f, 0.0f, //R

		        //y
	            0.0f, -2.0f, 0.0f,
                0.0f, 1.0f, 0.0f, //G
		        0.0f, 2.0f, 0.0f,
                0.0f, 1.0f, 0.0f, //G

		        //z
	            0.0f, 0.0f, 2.0f,
                0.0f, 0.0f, 1.0f,  //B
		        0.0f, 0.0f, -2.0f,
                0.0f, 0.0f, 1.0f,  //B

            };
            sh.UseShader();

            planeBufferID = GPU.GenerateBuffer(backGroundVertices);
            vertexID = GPU.GenerateBuffer(verts.ToArray());


            transID = Gl.glGetUniformLocation(sh.ID, "trans");
            projID = Gl.glGetUniformLocation(sh.ID, "projection");
            viewID = Gl.glGetUniformLocation(sh.ID, "view");

            DataID = Gl.glGetUniformLocation(sh.ID, "data");
            EyePositionID = Gl.glGetUniformLocation(sh.ID, "EyePosition_worldspace");
            AmbientLightID = Gl.glGetUniformLocation(sh.ID, "ambientLight");
            DiffuseLightID = Gl.glGetUniformLocation(sh.ID, "diffuseLight");
            SpecularLightID = Gl.glGetUniformLocation(sh.ID, "specularLight");

            LightPosID = Gl.glGetUniformLocation(sh.ID, "LightPosition_worldspace");
            checkTextureID = Gl.glGetUniformLocation(sh.ID, "checkTextureOut");
            checkLightID = Gl.glGetUniformLocation(sh.ID, "checkLightOut");

            cam = new Camera();
            cam.Reset(0, 2, 5, 0, 0, 0, 0, 1, 0);

            ProjectionMatrix = cam.GetProjectionMatrix();
            ViewMatrix = cam.GetViewMatrix();



            // Model matrix: apply transformations to the model
            List<mat4> transformations = new List<mat4>();
            transformations.Add(glm.scale(new mat4(1), new vec3(1, 2, 1)));
            transformations.Add(glm.rotate(-30.0f / 180 * 3.14f, new vec3(0, 1, 0)));
            ModelMatrix = MathHelper.MultiplyMatrices(transformations);

        }

        public void Draw()
        {
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
            Gl.glEnable(Gl.GL_DEPTH_TEST);
            Gl.glDepthFunc(Gl.GL_LESS);

            Gl.glLineWidth(1.5f);

            Gl.glUniform1f(checkTextureID, 0); //bb3t bool b 0 3shan 23rf hrsm b color wala texture
            Gl.glUniform1f(checkLightID, 0); //bb3t bool b 0 3shan 23rf ha apply light wla la2

            Gl.glUniformMatrix4fv(projID, 1, Gl.GL_FALSE, ProjectionMatrix.to_array());
            Gl.glUniformMatrix4fv(viewID, 1, Gl.GL_FALSE, ViewMatrix.to_array());
            Gl.glUniformMatrix4fv(transID, 1, Gl.GL_FALSE, ModelMatrix.to_array());
            Gl.glUniform3fv(EyePositionID, 1, cam.GetCameraPosition().to_array());


            Gl.glEnableVertexAttribArray(0);//pos
            Gl.glEnableVertexAttribArray(1);//color
            Gl.glEnableVertexAttribArray(2); //uv
            Gl.glEnableVertexAttribArray(3);//normal

            Gl.glBindBuffer(Gl.GL_ARRAY_BUFFER, planeBufferID);//plane

            Gl.glVertexAttribPointer(0, 3, Gl.GL_FLOAT, Gl.GL_FALSE, 6 * sizeof(float), (IntPtr)0);
            Gl.glVertexAttribPointer(1, 3, Gl.GL_FLOAT, Gl.GL_FALSE, 6 * sizeof(float), (IntPtr)(3 * sizeof(float)));

            Gl.glDrawArrays(Gl.GL_LINE_LOOP, 0, 4);
            Gl.glDrawArrays(Gl.GL_LINES, 4, 6);

            if (setInterpolationId == true)
            {
                vertexID = GPU.GenerateBuffer(interpolatedList.ToArray());
                setInterpolationId = false;
            }
            else
                vertexID = GPU.GenerateBuffer(verts.ToArray());
          
            Gl.glBindBuffer(Gl.GL_ARRAY_BUFFER, vertexID);//vertices

            Gl.glVertexAttribPointer(0, 3, Gl.GL_FLOAT, Gl.GL_FALSE, 11 * sizeof(float), IntPtr.Zero);//pos
            Gl.glVertexAttribPointer(1, 3, Gl.GL_FLOAT, Gl.GL_FALSE, 11 * sizeof(float), (IntPtr)(3 * sizeof(float)));//color
            Gl.glVertexAttribPointer(2, 2, Gl.GL_FLOAT, Gl.GL_FALSE, 11 * sizeof(float), (IntPtr)(6 * sizeof(float))); //uv
            Gl.glVertexAttribPointer(3, 3, Gl.GL_FLOAT, Gl.GL_FALSE, 11 * sizeof(float), (IntPtr)(8 * sizeof(float))); //normal

            Gl.glBindBuffer(Gl.GL_ELEMENT_ARRAY_BUFFER, indiciesBufferID);//indicies

            if (selectedVertex == true)
            {
                Gl.glPointSize(5);
                Gl.glDrawArrays(Gl.GL_POINTS, selectedVertexIndex, 1);
            }
            if (GraphicsForm.applyTexture)
                Gl.glUniform1f(checkTextureID, 1); //bb3t bool b 1 3shan arsm texture

            if (GraphicsForm.applyLight)
                Gl.glUniform1f(checkLightID, 1); //bb3t bool b 1 3shan arsm texture


            for (int i = 0; i < GraphicsForm.modesDict.Count; i++)
            {
                string key = GraphicsForm.modesDict.Keys.ElementAt(i);
                string[] arr = key.Split(' ');
                string drawingMode = arr[0];


                if (drawingMode == "GL_TRIANGLE_FAN")
                {
                    Gl.glDrawElements(Gl.GL_TRIANGLE_FAN, GraphicsForm.modesDict[key].Item2, Gl.GL_UNSIGNED_SHORT, (IntPtr)(sizeof(ushort) * (GraphicsForm.modesDict[key].Item1)));
                    if (GraphicsForm.applyTexture)
                    {
                        Gl.glDrawElements(Gl.GL_TRIANGLE_FAN, GraphicsForm.modesDict[key].Item2, Gl.GL_UNSIGNED_SHORT, (IntPtr)(sizeof(ushort) * (GraphicsForm.modesDict[key].Item1)));
                    }
                }
                else if (drawingMode == "GL_TRIANGLES")
                {
                    Gl.glDrawElements(Gl.GL_TRIANGLES, GraphicsForm.modesDict[key].Item2, Gl.GL_UNSIGNED_SHORT, (IntPtr)(sizeof(ushort) * (GraphicsForm.modesDict[key].Item1)));
                    if (GraphicsForm.applyTexture)
                    {
                        Gl.glDrawElements(Gl.GL_TRIANGLES, GraphicsForm.modesDict[key].Item2, Gl.GL_UNSIGNED_SHORT, (IntPtr)(sizeof(ushort) * (GraphicsForm.modesDict[key].Item1)));
                    }
                }
                else if (drawingMode == "GL_LINES")
                {
                    Gl.glDrawElements(Gl.GL_LINES, GraphicsForm.modesDict[key].Item2, Gl.GL_UNSIGNED_SHORT, (IntPtr)(sizeof(ushort) * (GraphicsForm.modesDict[key].Item1)));
                    if (GraphicsForm.applyTexture)
                    {
                        Gl.glDrawElements(Gl.GL_LINES, GraphicsForm.modesDict[key].Item2, Gl.GL_UNSIGNED_SHORT, (IntPtr)(sizeof(ushort) * (GraphicsForm.modesDict[key].Item1)));
                    }
                }
                else if (drawingMode == "GL_LINE_LOOP")
                {
                    Gl.glDrawElements(Gl.GL_LINE_LOOP, GraphicsForm.modesDict[key].Item2, Gl.GL_UNSIGNED_SHORT, (IntPtr)(sizeof(ushort) * (GraphicsForm.modesDict[key].Item1)));
                    if (GraphicsForm.applyTexture)
                    {
                        Gl.glDrawElements(Gl.GL_LINE_LOOP, GraphicsForm.modesDict[key].Item2, Gl.GL_UNSIGNED_SHORT, (IntPtr)(sizeof(ushort) * (GraphicsForm.modesDict[key].Item1)));
                    }
                }
                else if (drawingMode == "GL_POINTS")
                {
                    Gl.glPointSize(15);
                    Gl.glDrawElements(Gl.GL_POINTS, GraphicsForm.modesDict[key].Item2, Gl.GL_UNSIGNED_SHORT, (IntPtr)(sizeof(ushort) * (GraphicsForm.modesDict[key].Item1)));
                    //Gl.glUniform1f(checkTextureID, 0);
                }
                else if (drawingMode == "GL_LINE_STRIP")
                {
                    Gl.glDrawElements(Gl.GL_LINE_STRIP, GraphicsForm.modesDict[key].Item2, Gl.GL_UNSIGNED_SHORT, (IntPtr)(sizeof(ushort) * (GraphicsForm.modesDict[key].Item1)));
                    if (GraphicsForm.applyTexture)
                    {
                        Gl.glDrawElements(Gl.GL_LINE_STRIP, GraphicsForm.modesDict[key].Item2, Gl.GL_UNSIGNED_SHORT, (IntPtr)(sizeof(ushort) * (GraphicsForm.modesDict[key].Item1)));
                    }
                }
                else if (drawingMode == "GL_TRIANGLE_STRIP")
                {
                    Gl.glDrawElements(Gl.GL_TRIANGLE_STRIP, GraphicsForm.modesDict[key].Item2, Gl.GL_UNSIGNED_SHORT, (IntPtr)(sizeof(ushort) * (GraphicsForm.modesDict[key].Item1)));
                    if (GraphicsForm.applyTexture)
                    {
                        Gl.glDrawElements(Gl.GL_TRIANGLE_STRIP, GraphicsForm.modesDict[key].Item2, Gl.GL_UNSIGNED_SHORT, (IntPtr)(sizeof(ushort) * (GraphicsForm.modesDict[key].Item1)));
                    }
                }
            }

            Gl.glDisableVertexAttribArray(0);
            Gl.glDisableVertexAttribArray(1);
            Gl.glDisableVertexAttribArray(2);
            Gl.glDisableVertexAttribArray(3);
        }

        public void Update()
        {
            //update view and projection matrix
            cam.UpdateViewMatrix();
            ViewMatrix = cam.GetViewMatrix();
            ProjectionMatrix = cam.GetProjectionMatrix();

            //Keyframes
            if (GraphicsForm.runAnimation == true) 
            {
                int startKeyFrame = GraphicsForm.startFrame;
                int endKeyFrame = GraphicsForm.endFrame;// startKeyFrame + 1;
                time = startKeyFrame;

                    interpolatedList = new List<float>();
                    time += speed;

                    time %= endKeyFrame;
                    float alpha = (time - startKeyFrame) / (endKeyFrame - startKeyFrame);
                    
                    //to fill the main keyframes list just one time
                    if (initialzeList == 1)
                    { 
                        foreach (KeyFrame keyFrame in GraphicsForm.vertexKeyFramesDict.Values)
                        {
                            
                            tempKeyFrameList = new List<float>();
                            foreach (List<float> l in keyFrame.verticesDict.Values)
                                tempKeyFrameList.AddRange(l);

                            int numOfFrames = keyFrame.numOfInterpolatedFrames;
                            Tuple<int, List<float>> t = new Tuple<int, List<float>>(numOfFrames, tempKeyFrameList);
                            mainKeyFrameList.Add(t);
                        }
                        initialzeList = 0;
                    }

                    //to fill the interpolated list with the start, interpolated, end frames
                    for (int i = startKeyFrame; i < endKeyFrame; i++)
                    {
                        //ba7ot al start bta3e ele howa(0,0,0...)
                        interpolatedList.AddRange(mainKeyFrameList[i].Item2);
                        int count = mainKeyFrameList[i].Item2.Count;
                        //Item1: num of required interpolated frames
                        for (int j = 0; j < mainKeyFrameList[i + 1].Item1; j++)
                        {
                            int indx = 0;
                            for (; indx < mainKeyFrameList[i + 1].Item2.Count; indx++)
                            {
                                float interpolatedValue = (alpha* (mainKeyFrameList[i + 1].Item2[indx] - mainKeyFrameList[i].Item2[indx])) + mainKeyFrameList[i].Item2[indx];
                                interpolatedList.Add(interpolatedValue);
                            }

                            time += speed;
                            time %= endKeyFrame;
                            alpha = (time - startKeyFrame) / (endKeyFrame - startKeyFrame);
                        }
                    }
                    interpolatedList.AddRange(mainKeyFrameList[endKeyFrame].Item2);
              
       
            }
        }
        public void CleanUp()
        {
            sh.DestroyShader();
        }
    }
}