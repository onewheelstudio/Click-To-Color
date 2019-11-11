using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class ClickToColor : EditorWindow {

    [SerializeField]
    private Texture2D textureToEdit;
	private Texture2D tempTexture = null;
	private bool txReadable;

	private TextureImporterFormat txFormat;	
	
	private Color[] saveColors1;
	private Color[] saveColors2;
	private Color[] originalColors;

    [SerializeField]
    private List<Texture2D> savedTextureList = new List<Texture2D>();
    [SerializeField]
    private List<Color[]> savedColorsList = new List<Color[]>();
    private Vector2 scrollPos = Vector2.zero;

	private List<Color> colorList = new List<Color>();
	
	private Color[] colors;
	private List<ColorBlock> colorBlocks = new List<ColorBlock>();
	private List<string> textureFormats = new List<string>();
	private bool goodFormat = false;

	public bool autoAdjust = false;
    private bool isGrid = true;
    private bool creatingNew = false;
	//	int newRows = 2;
	//	int newColumns = 2;

	private int rows = 2;
	private int columns = 2;
	private int dimension = 64;
	private int gridSize = 2;
	private bool grayScale = false;
	private string fileName = "";
    private string savePath;

    private float winWidth;
    private GUISkin skin;
    private Texture2D OWS;
    private bool showAbout = false;

    [MenuItem ("Tools/Click To Color")]
	public static void  ShowWindow () {
		EditorWindow.GetWindow(typeof(ClickToColor));
		EditorWindow.GetWindow(typeof(ClickToColor)).minSize = new Vector2(400,320);
	}
	
	void OnEnable () 
	{

        savePath = Application.dataPath + "/../Assets/";

        winWidth = EditorWindow.GetWindow<ClickToColor>().position.width;
		//set list of acceptable texture formats
		textureFormats.Clear();
		textureFormats.Add ("ARGB32");
		textureFormats.Add ("RGBA32");
		textureFormats.Add ("RGB24");
		textureFormats.Add ("Alpha8");

		//Default file naming for created textures
		fileName = "CtC_" + UnityEngine.Random.Range(0,1000).ToString();

        Undo.undoRedoPerformed += UndoRedoPreformed;

        if (EditorGUIUtility.isProSkin)
            skin = EditorGUIUtility.Load("Assets/Click To Color/Resources/EasyUI_DarkSKin.guiskin") as GUISkin;
        else
            skin = EditorGUIUtility.Load("Assets/Click To Color/Resources/EasyUI_LightSkin.guiskin") as GUISkin;

        OWS = Resources.Load("Assets/Click To Color/Resources/OWS.png") as Texture2D;

    }

    void OnDisable()
	{
		ResetTxSetting(textureToEdit);
        Undo.undoRedoPerformed -= UndoRedoPreformed;
        ClearSavedData();
    }

    void UndoRedoPreformed()
    {
        Debug.Log("Undo or Redo");
    }

	//Create UI interface
	void OnGUI()
	{

        if (showAbout)
        {
            DrawAbout();
            return;
        }

        EditorGUILayout.Space();
        GUILayout.Box("Click To Color      ", skin.GetStyle("EditorHeading"));
        GUILayout.BeginArea(new Rect(position.width - 110, 3, 105, 40));
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("About"))
        {
            showAbout = true;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
        GUILayout.BeginVertical();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(OWS, skin.GetStyle("OWS"), GUILayout.MinWidth(40), GUILayout.MaxHeight(40)))
        {
            showAbout = true;

        }
        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        EditorGUILayout.Space();

        float uiWidth;
        if (textureToEdit != null)
        {
            EditorGUI.DrawPreviewTexture(new Rect(position.width / 2f + 12, 46, 244, 244), textureToEdit);
            uiWidth = position.width / 2 - 10;
        }
        else uiWidth = position.width - 10;

        //GUILayout.Label("Image may be blurry for Small Sizes", GUILayout.Height(15));
        if (textureToEdit != null && textureToEdit.width < 256)
        {
            GUILayout.BeginArea(new Rect(position.width / 2f + 18, 260, 230, 25), skin.box);
            GUILayout.Label("Image may be blurry for small sizes.", skin.GetStyle("label"), GUILayout.Height(15));
            GUILayout.EndArea();
        }

        GUILayout.Label("", GUILayout.Height(240));
        GUILayout.BeginArea(new Rect(10f, 50, uiWidth, 240), skin.box);

        EditorGUILayout.BeginHorizontal();
		EditorGUILayout.BeginVertical();

        EditorGUILayout.Space();
        if (!creatingNew)
        {
            tempTexture = EditorGUILayout.ObjectField("Texture to be edited", tempTexture, typeof(Texture2D), false, GUILayout.MaxWidth(uiWidth - 10)) as Texture2D;
            EditorGUILayout.Space();

            //Create new texture
            if (GUILayout.Button("Create New Texture", GUILayout.MaxWidth(uiWidth - 10)))
            {
                creatingNew = !creatingNew;
            }
            if (GUILayout.Button("Save Duplicate", GUILayout.MaxWidth(uiWidth - 10)))
            {
                MakeBackUp(textureToEdit);
            }
        }

        if (creatingNew)
        {
            //Set parameter for making a new texture
            GUILayout.Label("Grid Size (Power of 2)");
            gridSize = EditorGUILayout.IntSlider(gridSize, 2, 8);
            //gridSize = EditorGUILayout.IntField("Grid Size", gridSize, GUILayout.MaxWidth(position.width/2));
            GUILayout.Label("Texture Size (Power of 2)");
            dimension = EditorGUILayout.IntSlider(dimension, gridSize * 2, 1024);
            //dimension = EditorGUILayout.IntField("Texture Size",dimension, GUILayout.MaxWidth(position.width/2));
            EditorGUILayout.Space();
            grayScale = EditorGUILayout.ToggleLeft("Use Grayscale", grayScale, GUILayout.MaxWidth(uiWidth - 10));
            EditorGUILayout.Space();
            fileName = EditorGUILayout.TextField("Texture File Name", fileName, GUILayout.MaxWidth(uiWidth - 10));
            EditorGUILayout.Space();
            if (GUILayout.Button("Set Save Location", GUILayout.MaxWidth(uiWidth - 10)))
            {
                if(savePath == "")
                    savePath = Application.dataPath + "/../Assets/";

                savePath = EditorUtility.SaveFolderPanel("Save Files To...", savePath, fileName);
            }
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create", GUILayout.MaxWidth(uiWidth - 10)))
            {
                if (savePath == "")
                    savePath = Application.dataPath + "/../Assets/";

                CreateNewTexture();
                creatingNew = false;
            }
            if (GUILayout.Button("Cancel", GUILayout.MaxWidth(uiWidth - 10)))
            {
                creatingNew = false;
            }
            EditorGUILayout.EndHorizontal();
        }

        //Keep number of colors to a minimum
        //Auto adjust parameter to make texture a power of 2
        //And make sure that gridsize is a factor of the texture size
        //This prevents artifacts at the edges of color blocks and the texture as whole
		if(dimension > 0)
			dimension = Mathf.ClosestPowerOfTwo(dimension);
        if (dimension > gridSize)
            gridSize = NearestFactor(gridSize, dimension);
        if (gridSize > 8)
            gridSize = 8;

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        GUILayout.EndArea();

		//Check if there is texture to be edited
		if(tempTexture == null)
			return;
		
		//If new texture then set up for edit
		if(tempTexture != textureToEdit)
		{
			NewTexture(tempTexture);
			tempTexture = textureToEdit;
            ClearSavedData();
		}
		
		//Check the format of texture
		//Must be readable and be one of the "acceptable" formats
		if(CheckFormat(textureToEdit))
		{
			EditorGUILayout.Space();
            GUILayout.Label("", GUILayout.Height(rows * 18 + 27));
            GUILayout.BeginArea(new Rect(10, 300, position.width - 20, rows * 18 + 32), skin.box);
            GUILayout.Label("Edit Colors", skin.GetStyle("SectionHeading"));

			//Update colors in UI
			if(colorList.Count != rows * columns)
			{
				if(colorList.Count > rows * columns)
				{
					colorList.RemoveAt(colorList.Count - 1);
				}
				else
				{
					for(int i = 0; i < rows*columns; i++)
					{
						colorList.Add(new Color());
					}
				}
			}
            EditorGUILayout.Space();

            //Create Color Array
            for (int i = 0; i < rows; i++)
			{
				EditorGUILayout.BeginHorizontal();
				
				for(int j = 0; j < columns; j++)
				{
					Color tempColor;
					tempColor = EditorGUILayout.ColorField(colorList[i + j*rows]);
					
					if(tempColor != colorList[i + j*rows])
					{
                        Undo.RecordObject(textureToEdit, "Changing Color");
						ReplaceColors(textureToEdit, tempColor, rows - i - 1, j);
						colorList[i + j*rows] = tempColor;
                    } 
				}

                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }
		
		EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();

        //reverts texture to "original state" defined by colors before current edit
        if (GUILayout.Button("Revert to Original", GUILayout.MaxWidth(position.width/2)))
        {
            textureToEdit.SetPixels(originalColors);
            textureToEdit.Apply();
            MatchGridColors(textureToEdit);
            GetColors(textureToEdit);
        }
        if (GUILayout.Button("Save Revision", GUILayout.MaxWidth(position.width / 2)))
        {
            AddToSavedColors(colors);

            Texture2D tempTex = new Texture2D(textureToEdit.width, textureToEdit.height);
            tempTex = new Texture2D(textureToEdit.width, textureToEdit.width);
            tempTex.SetPixels(colors);
            tempTex.Apply();
            AddToSavedTextures(tempTex);
        }
        EditorGUILayout.EndHorizontal();


        DrawSavedVersions();
    }

    private void DrawAbout()
    {
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(OWS, skin.GetStyle("OWS"), GUILayout.MinWidth(128), GUILayout.MaxHeight(128)))
        {
            Application.OpenURL("https://www.youtube.com/onewheelstudio");
        }
        GUILayout.BeginVertical();
        EditorGUILayout.TextArea("One Wheel Studio", skin.GetStyle("EditorHeading"));
        EditorGUILayout.TextArea("Just one guy making low poly games, tutorial videos and the occasional Unity tool in his spare time.", skin.GetStyle("OWSText"));
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        EditorGUILayout.Space();

        EditorGUILayout.TextArea("You can support this free asset, by subscribing to my YouTube channel or joining the OWS Discord.", skin.GetStyle("OWSText"));
        GUILayout.BeginVertical();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (GUILayout.Button("YouTube", GUILayout.MinWidth(150), GUILayout.MinHeight(30)))
        {
            Application.OpenURL("https://www.youtube.com/onewheelstudio");
        }
        //if (GUILayout.Button("Patreon", GUILayout.MinWidth(150)))
        //{
        //    Application.OpenURL("https://www.youtube.com/onewheelstudio");
        //}
        if (GUILayout.Button("Discord", GUILayout.MinWidth(150), GUILayout.MinHeight(30)))
        {
            Application.OpenURL("https://discord.gg/mBQRTHt");
        }
        GUILayout.EndVertical();
        EditorGUILayout.Space();

        GUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        if (GUILayout.Button("Close", GUILayout.MinHeight(20)))
        {
            showAbout = false;
        }
    }

    private void CreateNewTexture()
    {
        rows = gridSize;
        columns = gridSize;

        tempTexture = CreateNewTexture(rows, dimension);
        NewTexture(tempTexture);
        tempTexture = textureToEdit;
        fileName = "CtC_" + UnityEngine.Random.Range(0, 1000).ToString();
        ClearSavedData();
    }

    private void AddToSavedColors(Color[] colors)
    {
        Color[] newColors = new Color[colors.Length];
        colors.CopyTo(newColors, 0); //create new array with new reference - fixes revision weirdness
        savedColorsList.Insert(0, newColors);
    }

    private void AddToSavedTextures(Texture2D texture)
    {
        savedTextureList.Insert(0, texture);
    }

    private void ClearSavedData()
    {
        savedColorsList.Clear();
        savedTextureList.Clear();
    }

    private void DrawSavedVersions()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.BeginVertical();
        for (int i = 0; i < savedColorsList.Count; i++)
        {

            //GUILayout.Label("", GUILayout.Height(50));
            GUILayout.BeginArea(new Rect(10, 60 * i + 10, position.width -20, 50), skin.box);
            float buttonWidth = (position.width) * 0.4f;

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load Revision " + (savedColorsList.Count - i).ToString(), GUILayout.MaxWidth(buttonWidth)))
            {
                Debug.Log(savedColorsList.Count  + " " + savedTextureList.Count);
                textureToEdit.SetPixels(savedColorsList[i]);
                textureToEdit.Apply();
                MatchGridColors(textureToEdit);
                GetColors(textureToEdit);
            }
            if (GUILayout.Button("Remove Revision " + (savedColorsList.Count - i).ToString(), GUILayout.MaxWidth(buttonWidth)))
            {
                savedColorsList.RemoveAt(i);
                savedTextureList.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();

            float imageXOffset = buttonWidth * 2 + 40;
            if (savedTextureList.Count > i && savedTextureList[i] != null)
                EditorGUI.DrawPreviewTexture(new Rect(imageXOffset, 15 + 60 * i, 35, 35), savedTextureList[i]);

            Rect rect = GUILayoutUtility.GetRect(position.width - 20, 60);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }

	//Sets up the texture and the editor script for new texture
	void NewTexture(Texture2D tempTx)
	{
		if(tempTx == null)
			return;
		
		MakeReadable(tempTx);
		//MakeBackUp(tempTx);
		
		//cache current colors for revert
		originalColors = tempTx.GetPixels();

        if (isGrid)
            MatchGridColors(tempTx);
        else
            MatchNonGridColors(tempTx);

		GetColors(tempTx);
		
		int txWidth;
		int txHeight;
		txWidth = tempTx.width;
		txHeight = tempTx.height;
		
		//reset saved textures
		//saveTexture1 = new Texture2D(txWidth,txHeight);
		//saveTexture2 = new Texture2D(txWidth,txHeight);
		//saveColors1 = originalColors;
		//saveColors2 = originalColors;
		//saveTexture1.SetPixels(saveColors1);
		//saveTexture1.Apply();
		//saveTexture2.SetPixels(saveColors2);
		//saveTexture2.Apply();
		
		//will only get here with new texture
		//will reset parameter of old texture
		if(textureToEdit != null)
			ResetTxSetting(textureToEdit);
		
		textureToEdit = tempTx;
	}
       
    //Makes the texture file readable for editing purposes
    void MakeReadable(Texture2D _texture)
	{
		string path;
		path = AssetDatabase.GetAssetPath(_texture);
		
		TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);
		//Save setting to reset after disable or unload of texture
		txReadable = importer.isReadable;
		//txFormat = importer.textureFormat;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
		importer.isReadable = true;
		//importer.textureFormat = TextureImporterFormat.RGBA32;
		AssetDatabase.ImportAsset(path);
	}

	//Check to see if the format of the texture is appropriate for editing
	private bool CheckFormat(Texture2D _texture)
	{
		goodFormat = false;
		
		for(int i = 0; i < textureFormats.Count; i++)
		{
			if(textureFormats[i].ToString() == _texture.format.ToString())
			{
				goodFormat = true;
			}
		}		
		try
		{
			textureToEdit.GetPixels();
		}
		catch (UnityException e)
		{
			//Next line to prevent Unity from throwing occasional errors.
			e.GetType();
			
			goodFormat = false;
		}
		
		//If not good formating then make it good formatting
		if(!goodFormat && _texture != null)
		{
			MakeReadable(_texture);
		}

        return goodFormat;
	}
	
	//Reset readable and format to original
	void ResetTxSetting(Texture2D txTemp)
	{
        //Debug.Log("Resetting");
        if (txTemp == null)
			return;
		
		string path;
		path = AssetDatabase.GetAssetPath(txTemp);
		byte[] pngData = txTemp.EncodeToPNG();
		
		if(pngData != null)
		{
			//Debug.Log("Saving Data");
			File.WriteAllBytes(path,pngData);
			AssetDatabase.Refresh();
		}
		
		TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);
		if(path != null)
		{
			//Debug.Log("reseting");
			importer.isReadable = txReadable;
            importer.textureCompression = TextureImporterCompression.Compressed;
            //importer.textureFormat = txFormat;
			AssetDatabase.ImportAsset(path);
		}
	}

    private void MatchNonGridColors(Texture2D tempTx)
    {
        Color[] colors = tempTx.GetPixels();
        colorList.Clear();

        foreach(Color c in colors)
        {
            if (!colorList.Contains(c))
            {
                colorList.Add(c);
                Debug.Log("New Color " + c);
            }
        }

        Debug.Log("Number of Colors " + colorList.Count);
    }

    //Searches the texture for different colors
    //Sets up "grid" of colors
    void MatchGridColors(Texture2D _texture)
	{
		int tempRows = 1 ;
		int tempColumns = 1;
		Color tempColor1;
		Color tempColor2;

		//Gets numbers of rows
		tempColor1 = _texture.GetPixel(0,0);
		for(int i = 0; i < _texture.width; i++)
		{
			tempColor2 = _texture.GetPixel(i,0);
			if(!CompareColor(tempColor1,tempColor2))
			{
				tempRows++;
				tempColor1 = tempColor2;
			}

			if(tempRows > 13)
				break;
		}

		//gets number of columns
		tempColor1 = _texture.GetPixel(0,0);
		for(int i = 0; i < _texture.height; i++)
		{
			tempColor2 = _texture.GetPixel(0,i);
			if(!CompareColor(tempColor1,tempColor2))
			{
				tempColumns++;
				tempColor1 = tempColor2;
			}
			if(tempColumns > 13)
				break;
		}

		//Sets global variables to match findings
		rows = tempRows;
		columns = tempColumns;

		//steps through the grid to find the different colors and stores them on ColorList
		//Steps are based on row and columns numbers as well as the grid being uniform size
		int wFirstStep;
		int hFirstStep;
		int widthStep;
		int heightStep;
		widthStep = Mathf.RoundToInt(_texture.width/tempRows);
		wFirstStep = Mathf.RoundToInt(widthStep/2);
		heightStep = Mathf.RoundToInt(_texture.height/tempColumns);
		hFirstStep = Mathf.RoundToInt(heightStep/2);
		
        if(colorList != null)
		    colorList.Clear();
		
		for(int i = 0; i < tempRows; i++)
		{
			for(int j = 0; j < tempColumns; j++)
			{
				int xPos;
				int yPos;
				xPos = wFirstStep + widthStep * i;
				yPos = _texture.height - (hFirstStep + heightStep * j);
				Color tempColor;
				tempColor = _texture.GetPixel(xPos,yPos);
				
				colorList.Add (tempColor);
			}
		}
	}

	//Compares colors to determine if colors have changed
	bool CompareColor(Color color1, Color color2)
	{		
		if(color1.r != color2.r)
		{
			return false;
		}
		else if(color1.g != color2.g)
		{
			return false;
		}
		else if(color1.b != color2.b)
		{
			return false;
		}
		else if(color1.a != color2.a)
		{
			return false;
		}
		else
		{
			return true;
		}
	}

	//Simply grabs the colors from the texture and caches them in an array
	void GetColors(Texture2D _texture)
	{
		//colors is array of colors
		colors = _texture.GetPixels();
	}

	//Caches the coordinates of colors
	//This was an improvement to prevent erros when two blocks had identical colors
	void GetColorCoords(Texture2D _texture)
	{
		Color tempColor1;
		Color tempColor2;

		tempColor1 = _texture.GetPixel(0,0);
		for(int i = 0; i < _texture.width; i++)
		{
			for(int j = 0; j < _texture.height; j++)
			{
				tempColor2 = _texture.GetPixel(i,j);
				if(!CompareColor(tempColor1,tempColor2))
				{
					foreach(ColorBlock cb in colorBlocks)
					{
						if(tempColor2 == cb.color)
						{
							cb.colorCoordinates.Add (new Vector2(i,j));
							cb.colorIndex.Add(i + j);
							break;
							
						}							
					}

					ColorBlock tempCB = new ColorBlock();
					tempCB.color = tempColor2;
					tempCB.colorCoordinates.Add (new Vector2(i,j));
					tempCB.colorIndex.Add(i+j);
					colorBlocks.Add (tempCB);
					tempCB.indexNum = GetColorIndex(tempColor2);
					//Debug.Log ("New Color ");// + i + " , " +j + " " + tempColor2);
					//ColorList.Add(tempColor2);

					tempColor1 = tempColor2;
				}
				else
				{
					foreach(ColorBlock cb in colorBlocks)
					{
						if(tempColor2 == cb.color)
						{
							cb.colorCoordinates.Add (new Vector2(i,j));
							cb.colorIndex.Add(i + j);
							break;

						}							
					}
				}
			}
		}
	}

	//Helper function to track colors in texture compared to colors displayed in UI
	int GetColorIndex(Color _color)
	{
		for(int i = 0; i < colorList.Count; i++)
		{
			if(_color == colorList[i])
				return i;
		}
		return 0;
	}

	//Old replace function that simply replaces one color in the texture with another
	void ReplaceColors(Color oldColor, Color newColor)
	{

		for(int i = 0; i < colors.Length; i++)
		{
			if(colors[i] == oldColor)
			{
				colors[i] = newColor;
			}
		}
		
		textureToEdit.SetPixels(colors);
		textureToEdit.Apply();
		
		//Undo slows down editor window too significantly to use
		///Undo.RecordObject(textureToEdit, "Color Change");
		//		textureToEdit.SetPixels(colors);
		//		textureToEdit.Apply();
	}

	//new improvement replacment that replaces using the coordinates of the colors
	void ReplaceColors(Texture2D _texture,Color newColor,int row, int column)
	{
		int iMin = Mathf.CeilToInt(_texture.width/rows * column);
		int iMax = Mathf.CeilToInt(_texture.width/rows * (column + 1));
		int jMin = Mathf.FloorToInt(_texture.height/columns * row);
		int jMax = Mathf.CeilToInt(_texture.height/columns * (row + 1));

		if(jMax > _texture.height)
			jMax = _texture.height;
		if(iMax > _texture.width)
			iMax = _texture.width;

		for(int i = iMin; i < iMax ; i++)
		{
			for(int j = jMin; j < jMax; j++)
			{
				int coordinate = _texture.width * j + i;
				if(coordinate < colors.Length)
		       	 colors[coordinate] = newColor;
			}
		}
		
		_texture.SetPixels(colors);
		_texture.Apply();
		
		//Undo slows down editor window too significantly to use
		///Undo.RecordObject(textureToEdit, "Color Change");
		//		textureToEdit.SetPixels(colors);
		//		textureToEdit.Apply();
	}

	//Creates new texture based on gridsize and pixel dimensions
	Texture2D CreateNewTexture(int _gridSize, int _size)
	{
		Texture2D newTexture;
		newTexture = new Texture2D(_size,_size,TextureFormat.ARGB32,false);
		newTexture.SetPixels(SetColors(_size));
		colors = newTexture.GetPixels();
		newTexture.Apply(false);

		//sets each pixel color
		for(int i = 0; i < _gridSize; i++)
		{
			for(int j = 0; j < _gridSize; j++)
			{
				float tempValue;
				Color tempColor;

				if(grayScale)
				{
					tempValue = 1f / (_gridSize * _gridSize) * (i + j);
					tempColor = new Color(tempValue,tempValue,tempValue,1f);;
				}
				else
					tempColor = new Color(UnityEngine.Random.Range(0f,1f), UnityEngine.Random.Range(0f,1f), UnityEngine.Random.Range(0f,1f),1f);

				ReplaceColors(newTexture,tempColor,i,j);
			}
		}

		//setcolors
		byte[] bytes = newTexture.EncodeToPNG();


		//Saves as file
		File.WriteAllBytes(savePath + "/" + fileName +".png",bytes);
		string tempPath;
		tempPath = AssetDatabase.GetAssetPath(newTexture);
        UnityEngine.Object.DestroyImmediate(newTexture);
		AssetDatabase.Refresh();

		string [] tempArray;
		tempArray = AssetDatabase.FindAssets(fileName);

		string path;
		path = AssetDatabase.GUIDToAssetPath(tempArray[0]);

		Debug.Log(path);

		newTexture = AssetDatabase.LoadAssetAtPath(path,typeof(Texture2D)) as Texture2D;

		return newTexture;
	}
	//Sets colors for created texture
	Color[] SetColors(int _size)
	{
		Color[] newColors = new Color[_size * _size];
		
		for(int i = 0; i < newColors.Length; i++)
		{
			newColors[i] = new Color(UnityEngine.Random.Range(0f,1f), UnityEngine.Random.Range(0f,1f), UnityEngine.Random.Range(0f,1f), UnityEngine.Random.Range(0f,1f));
		}
		
		return newColors;
	}

	//Each time a texture is loaded a backup copy is saved
	void MakeBackUp(Texture2D _texture)
	{
		string tempName;
        tempName = _texture.name + "_Duplicate_";
        tempName += System.DateTime.Now.ToShortDateString().Replace(":", "").Replace("/","_");
        tempName += "_" + System.DateTime.Now.ToLongTimeString().Replace(":", "_").Replace("/", "_");

        //Check if folder exists. If not create it.
        if (!System.IO.Directory.Exists(Application.dataPath + "/../Assets/Click To Color/CtC Duplicates/"))
			AssetDatabase.CreateFolder("Assets/Click To Color", "CtC Duplicates");

		//checks to see if file already is exists if so increments file name
		tempName = GetFileName(tempName, 0); //, Application.dataPath + "/../Assets/CtC BackUps/"
        byte[] bytes = _texture.EncodeToPNG();			
		File.WriteAllBytes(Application.dataPath + "/../Assets/Click To Color/CtC Duplicates/" + tempName +".png",bytes);
		AssetDatabase.Refresh();
	}

	//recursive function to name file with increasing number
	string GetFileName(string _texName, int _try)
	{
		_try++;

		string tempName;
	    tempName = _texName + "_" + _try;

        if (!System.IO.File.Exists(Application.dataPath + "/../Assets/CtC BackUps/" + tempName + ".png"))
		{
			return tempName;
		}
		else
			return GetFileName(_texName,_try);
	}



	//recursive function to get nearest factor
	int NearestFactor(int _factor, int _number)
	{
		if(_number % _factor == 0)
			return _factor;
		else
			return NearestFactor(_factor + 1, _number);
	}

	//class used to store coordinates of different colors blocks
	public class ColorBlock
	{
		public Color color = new Color();
		public int indexNum;
		public List<Vector2> colorCoordinates = new List<Vector2>();
		public List<int> colorIndex = new List<int>();
	}	
}

