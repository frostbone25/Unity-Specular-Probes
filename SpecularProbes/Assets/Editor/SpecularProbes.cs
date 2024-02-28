#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

namespace CustomEditorTools
{
    /// <summary>
    /// Editor tool that spawns emissive meshes as a substitue for specular highlights when baking reflection probes.
    /// </summary>
    public class SpecularProbes : EditorWindow
    {
        //GUI related
        private static int guiSectionSpacePixels = 10;
        private Vector2Int windowSize = new Vector2Int(350, 490);
        private GUIStyle bgLightGrey;
        private Light[] sceneLights;
        private bool drawGizmos = false;

        //options
        private bool includeRealtimeLights = false;
        private bool includeMixedLights = false;
        private bool includeBakedLights = true;

        private bool includeAreaLights = true;
        private bool includePointLights = true;
        private bool includeSpotLights = true;

        private float point_lightSize = 0.1f;
        private float point_lightIntensityMultiplier = 1.0f;

        private float spot_lightSize = 0.1f;
        private float spot_lightIntensityMultiplier = 1.0f;

        private bool area_doubleSided = false;
        private float area_lightThickness = 0.01f;
        private float area_lightIntensityMultiplier = 1.0f;

        //main logic
        private List<GameObject> specularObjects = new List<GameObject>();

        //add a menu item at the top of the unity editor toolbar
        [MenuItem("Custom Editor Tools/Specular Probes")]
        public static void ShowWindow()
        {
            //get the window and open it
            GetWindow(typeof(SpecularProbes));
        }

        /// <summary>
        /// When the current window is pressed/focused on by the user.
        /// </summary>
        void OnFocus()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        /// <summary>
        /// When the window is closed.
        /// </summary>
        void OnDestroy()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        /// <summary>
        /// GUI display function for the window
        /// </summary>
        void OnGUI()
        {
            maxSize = windowSize;
            minSize = windowSize;

            //get a nice little fancy grey bar for titles
            if (bgLightGrey == null)
            {
                bgLightGrey = new GUIStyle(EditorStyles.label);
                bgLightGrey.normal.background = Texture2D.linearGrayTexture;
            }

            //|||||||||||||||||||||||||| SPECULAR PROBES ||||||||||||||||||||||||||
            //|||||||||||||||||||||||||| SPECULAR PROBES ||||||||||||||||||||||||||
            //|||||||||||||||||||||||||| SPECULAR PROBES ||||||||||||||||||||||||||
            GUILayout.BeginVertical(bgLightGrey);
            GUILayout.Label("Specular Probes", EditorStyles.whiteLargeLabel);
            GUILayout.EndVertical();
            GUILayout.Space(guiSectionSpacePixels);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("Light Modes", EditorStyles.boldLabel);
            includeRealtimeLights = EditorGUILayout.Toggle("Include Realtime Lights", includeRealtimeLights);
            includeMixedLights = EditorGUILayout.Toggle("Include Mixed Lights", includeMixedLights);
            includeBakedLights = EditorGUILayout.Toggle("Include Baked Lights", includeBakedLights);
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Label("Light Types", EditorStyles.boldLabel);
            includeAreaLights = EditorGUILayout.Toggle("Reflect Area Lights", includeAreaLights);
            includePointLights = EditorGUILayout.Toggle("Reflect Point Lights", includePointLights);
            includeSpotLights = EditorGUILayout.Toggle("Reflect Spot Lights", includeSpotLights);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            //|||||||||||||||||||||||||| SETTINGS ||||||||||||||||||||||||||
            //|||||||||||||||||||||||||| SETTINGS ||||||||||||||||||||||||||
            //|||||||||||||||||||||||||| SETTINGS ||||||||||||||||||||||||||
            GUILayout.Space(guiSectionSpacePixels);
            GUILayout.BeginVertical(bgLightGrey);
            GUILayout.Label("Settings", EditorStyles.whiteLargeLabel);
            GUILayout.EndVertical();

            GUILayout.Space(guiSectionSpacePixels);
            drawGizmos = EditorGUILayout.Toggle("Draw Gizmos", drawGizmos);
            GUILayout.Space(guiSectionSpacePixels);

            if (includePointLights)
            {
                GUILayout.Label("[Point Lights]", EditorStyles.boldLabel);
                point_lightSize = EditorGUILayout.FloatField("Emissive Size", point_lightSize);
                point_lightIntensityMultiplier = EditorGUILayout.FloatField("Intensity Multiplier", point_lightIntensityMultiplier);
                GUILayout.Space(guiSectionSpacePixels);
            }

            if (includeSpotLights)
            {
                GUILayout.Label("[Spot Lights]", EditorStyles.boldLabel);
                spot_lightSize = EditorGUILayout.FloatField("Emissive Size", spot_lightSize);
                spot_lightIntensityMultiplier = EditorGUILayout.FloatField("Intensity Multiplier", spot_lightIntensityMultiplier);
                GUILayout.Space(guiSectionSpacePixels);
            }

            if (includeAreaLights)
            {
                GUILayout.Label("[Area Lights]", EditorStyles.boldLabel);
                area_doubleSided = EditorGUILayout.Toggle("Is Double Sided", area_doubleSided);

                if(area_doubleSided)
                    area_lightThickness = EditorGUILayout.FloatField("Emissive Thickness", area_lightThickness);

                area_lightIntensityMultiplier = EditorGUILayout.FloatField("Intensity Multiplier", area_lightIntensityMultiplier);
                GUILayout.Space(guiSectionSpacePixels);
            }

            //|||||||||||||||||||||||||| BAKE ||||||||||||||||||||||||||
            //|||||||||||||||||||||||||| BAKE ||||||||||||||||||||||||||
            //|||||||||||||||||||||||||| BAKE ||||||||||||||||||||||||||
            GUILayout.Space(guiSectionSpacePixels);
            GUILayout.BeginVertical(bgLightGrey);
            GUILayout.Label("Bake", EditorStyles.whiteLargeLabel);
            GUILayout.EndVertical();
            GUILayout.Space(guiSectionSpacePixels);

            if (GUILayout.Button("Bake Specular Reflection Probes"))
            {
                PlaceEmissives();
                BakeReflectionProbes();
                DestroyEmissives();
            }

            if (GUILayout.Button("Bake Reflection Probes"))
            {
                BakeReflectionProbes();
            }

            //get all of the lights in the scene
            sceneLights = FindObjectsOfType<Light>();
        }

        //||||||||||||||||||||||||||||||||||||||||||||||||| MAIN LOGIC |||||||||||||||||||||||||||||||||||||||||||||||||
        //||||||||||||||||||||||||||||||||||||||||||||||||| MAIN LOGIC |||||||||||||||||||||||||||||||||||||||||||||||||
        //||||||||||||||||||||||||||||||||||||||||||||||||| MAIN LOGIC |||||||||||||||||||||||||||||||||||||||||||||||||

        /// <summary>
        /// Bakes the reflection probes for the current active scene.
        /// </summary>
        private void BakeReflectionProbes()
        {
            //get all reflection probes in the scene
            ReflectionProbe[] probes = FindObjectsOfType<ReflectionProbe>();

            //get the current active scene and its path on the disk.
            UnityEngine.SceneManagement.Scene activeScene = EditorSceneManager.GetActiveScene();
            string sceneFilePath = activeScene.path;
            string sceneFolderPath = sceneFilePath.Remove(sceneFilePath.Length - ".unity".Length);

            //make sure the scene is saved or valid before we continue
            if (activeScene.IsValid() == false || string.IsNullOrEmpty(activeScene.path))
            {
                string message = "Scene is not valid! Be sure to save the scene before baking!";
                EditorUtility.DisplayDialog("Error", message, "OK");
                return; //dont continue
            }

            //iterate through all reflection probes in the scene
            for(int i = 0; i < probes.Length; i++)
            {
                //bake each reflection probe with a progress bar
                EditorUtility.DisplayProgressBar("Baking Specular Probes", string.Format("Baking {0}...", probes[i].name), i / probes.Length);
                string reflectionProbePath = sceneFolderPath + "/" + probes[i].name + ".exr";
                Lightmapping.BakeReflectionProbe(probes[i], reflectionProbePath);
            }

            //get rid of the progress bar because we are done.
            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// Spawns an emissive mesh object for each light.
        /// </summary>
        private void PlaceEmissives()
        {
            //iterate through all scene lights in the scene
            //note: sceneLights is filled in OnGUI
            foreach (Light light in sceneLights)
            {
                //get our cases for the lights that we are including in the specular probe bakes.
                bool case1 = light.lightmapBakeType == LightmapBakeType.Baked && includeBakedLights;
                bool case2 = light.lightmapBakeType == LightmapBakeType.Mixed && includeMixedLights;
                bool case3 = light.lightmapBakeType == LightmapBakeType.Realtime && includeRealtimeLights;

                //if the current light passes any of the cases
                if (case1 || case2 || case3)
                {
                    //spawn a corresponding emissive to match its light source and add it to 'specularObjects'.

                    if (light.type == LightType.Point && includePointLights)
                        GetPointLightMesh(light);

                    if (light.type == LightType.Spot && includeSpotLights)
                        GetSpotLightMesh(light);

                    if (light.type == LightType.Area && includeAreaLights)
                        GetAreaLightMesh(light);
                }
            }
        }

        /// <summary>
        /// Destroys all emissive mesh objects.
        /// </summary>
        private void DestroyEmissives()
        {
            //destroy all specular objects in the array that were spawned.
            for (int i = 0; i < specularObjects.Count; i++)
                DestroyImmediate(specularObjects[i]);

            //clear the array.
            specularObjects.Clear();
        }

        private void GetPointLightMesh(Light light)
        {
            GameObject sphereMeshObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            SetupMesh(sphereMeshObject, light, point_lightIntensityMultiplier, new Vector3(point_lightSize, point_lightSize, point_lightSize));
        }

        private void GetSpotLightMesh(Light light)
        {
            GameObject spotMeshObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            SetupMesh(spotMeshObject, light, spot_lightIntensityMultiplier, new Vector3(spot_lightSize, spot_lightSize, spot_lightSize));
        }

        private void GetAreaLightMesh(Light light)
        {
            GameObject areaMeshObject = GameObject.CreatePrimitive(area_doubleSided ? PrimitiveType.Cube : PrimitiveType.Quad);
            SetupMesh(areaMeshObject, light, area_lightIntensityMultiplier, new Vector3(light.areaSize.x, light.areaSize.y, area_doubleSided ? area_lightThickness : -1.0f));
        }

        /// <summary>
        /// Create an emissive material to emulate the light source.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="intensity"></param>
        /// <returns></returns>
        private static Material GetLightMaterial(Color color, float intensity)
        {
			#if UNITY_2018_1_OR_NEWER
			// URP support
			if (GraphicsSettings.currentRenderPipeline != null)
			{
				Material sphereMaterial = new Material(GraphicsSettings.currentRenderPipeline.defaultParticleMaterial.shader);
				return sphereMaterial;
			}
			else
			#endif
			{
				Material sphereMaterial = new Material(Shader.Find("Standard"));

				sphereMaterial.EnableKeyword("_EMISSION");
				sphereMaterial.SetColor("_Color", color);
				sphereMaterial.SetColor("_EmissionColor", color * intensity);

				return sphereMaterial;
			}
        }

        /// <summary>
        /// Spawn a gameobject with the light emissive material, and the mesh to match the shape of the light and add it to specularOjects.
        /// </summary>
        /// <param name="meshObject"></param>
        /// <param name="light"></param>
        /// <param name="multiplier"></param>
        /// <param name="size"></param>
        private void SetupMesh(GameObject meshObject, Light light, float multiplier, Vector3 size)
        {
            MeshRenderer meshRenderer = meshObject.GetComponent<MeshRenderer>();

            meshRenderer.sharedMaterial = GetLightMaterial(light.color, light.intensity * multiplier);
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.allowOcclusionWhenDynamic = false;
            meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            meshObject.transform.localScale = size;
            meshObject.transform.position = light.transform.position;
            meshObject.transform.rotation = light.transform.rotation;

            GameObjectUtility.SetStaticEditorFlags(meshObject, StaticEditorFlags.ReflectionProbeStatic);

            specularObjects.Add(meshObject);
        }

        //||||||||||||||||||||||||||||||||||||||||||||||||| GIZMOS |||||||||||||||||||||||||||||||||||||||||||||||||
        //||||||||||||||||||||||||||||||||||||||||||||||||| GIZMOS |||||||||||||||||||||||||||||||||||||||||||||||||
        //||||||||||||||||||||||||||||||||||||||||||||||||| GIZMOS |||||||||||||||||||||||||||||||||||||||||||||||||

        /// <summary>
        /// For drawing gizmos within the scene.
        /// </summary>
        /// <param name="sceneView"></param>
        void OnSceneGUI(SceneView sceneView)
        {
            //if gizmos are disabled don't continue
            if (!drawGizmos)
                return;

            //if the array is not initalized don't continue
            if (sceneLights == null)
                return;

            //get the current scene camera position
            Vector3 sceneCameraPosition = SceneView.GetAllSceneCameras()[0].transform.position;

            //iterate through all of the lights in the scene.
            for (int i = 0; i < sceneLights.Length; i++)
            {
                //get the current light
                Light currentLight = sceneLights[i];

                //get our cases for the lights that we are including in the specular probe bakes.
                bool case1 = currentLight.lightmapBakeType == LightmapBakeType.Baked && includeBakedLights;
                bool case2 = currentLight.lightmapBakeType == LightmapBakeType.Mixed && includeMixedLights;
                bool case3 = currentLight.lightmapBakeType == LightmapBakeType.Realtime && includeRealtimeLights;

                //if either of them pass then draw a gizmo
                if(case1 || case2 || case3)
                {
                    //get current light position
                    Vector3 currentLightPosition = currentLight.transform.position;
                    Vector3 gizmoNormal = currentLightPosition - sceneCameraPosition; //for disc handle since spheres dont exist...

                    //set the handle color to the light
                    Handles.color = currentLight.color;

                    //draw gizmos for each light type
                    if (currentLight.type == LightType.Spot)
                    {
                        Handles.DrawSolidDisc(currentLightPosition, gizmoNormal, spot_lightSize);
                    }
                    else if (currentLight.type == LightType.Point)
                    {
                        Handles.DrawSolidDisc(currentLightPosition, gizmoNormal, point_lightSize);
                    }
                    else if(currentLight.type == LightType.Area)
                    {
                        Vector3[] rectangleVerts = new Vector3[4];
                        rectangleVerts[0] = currentLight.transform.TransformPoint(new Vector3(currentLight.areaSize.x / 2, currentLight.areaSize.y / 2, 0));
                        rectangleVerts[1] = currentLight.transform.TransformPoint(new Vector3(currentLight.areaSize.x / 2, -currentLight.areaSize.y / 2, 0));
                        rectangleVerts[2] = currentLight.transform.TransformPoint(new Vector3(-currentLight.areaSize.x / 2, -currentLight.areaSize.y / 2, 0));
                        rectangleVerts[3] = currentLight.transform.TransformPoint(new Vector3(-currentLight.areaSize.x / 2, currentLight.areaSize.y / 2, 0));

                        Handles.DrawSolidRectangleWithOutline(rectangleVerts, currentLight.color, currentLight.color);
                    }
                }
            }

            HandleUtility.Repaint();
        }
    }
}

#endif