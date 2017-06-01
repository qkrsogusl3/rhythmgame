﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using System.Text;
using System.IO;

public class CStageEditTool : EditorWindow
{
    private const string PATH_EDIT_DATA = "Assets/Resources/StageData";

    [MenuItem("Tools/GameEditor/ThemeEditTool %5")]
    public static void OpenWindow()
    {
        var tWindow = GetWindow<CStageEditTool>();
        tWindow.minSize = new Vector2(800, 550);
        tWindow.Show();
    }

    private string mCreateDataName = string.Empty;
    private float mAddSeqBeat = 0.0f;
    private string mAddActionCode = string.Empty;
    private int mSelectedActionCodeIndex = 0;
    private int mCopySeqStartIndex = 0;
    private int mCopySeqEndIndex = 0;
    private List<bool> mSeqIsExpanded = new List<bool>();

    private CStageData mEditData = null;

    private Vector2 mSequenceScrollViewPos = Vector2.zero;


    public enum SequenceEditTap
    {
        Edit, Template,
    }
    private SequenceEditTap mCurrentTap = SequenceEditTap.Edit;

    private void OnEnable()
    {
    }


    private void OnGUI()
    {
        
        DrawCreateEditData();

        GUILayout.Space(10);

        if (mEditData == null)
            return;


        GUILayout.BeginHorizontal();
        mEditData.BPM = CCustomField.IntField("BPM : ", mEditData.BPM);
        EditorGUILayout.LabelField(string.Format("BPS : {0}", mEditData.BPS), GUILayout.Width(120));
        mEditData.StartBeatOffset = CCustomField.FloatField("StartBeatOffset : ", mEditData.StartBeatOffset, 100);
        mEditData.PerfectRange = CCustomField.FloatField("Perfect Range : ", mEditData.PerfectRange, 100);

        if (GUILayout.Button("ToJson", GUILayout.Width(50)))
        {
            Debug.Log(JsonUtility.ToJson(mEditData));
        }
        GUILayout.EndHorizontal();


        //Audio Resources
        GUILayout.BeginHorizontal();
        mEditData.Music = CCustomField.ObjectField<AudioClip>("Music : ", mEditData.Music, tFieldWidth: 200);
        GUILayout.EndHorizontal();


        GUILayout.BeginHorizontal();
        GUILayout.Label("Evaluation [Low][Mid][High] : ", GUILayout.Width(180));

        mEditData.EvaluationLowRatio = EditorGUILayout.FloatField(mEditData.EvaluationLowRatio,GUILayout.Width(50));
        GUILayout.Box(mEditData.EvaluationMiddleRatio.ToString(), GUILayout.Width(50));
        mEditData.EvaluationHighRatio = EditorGUILayout.FloatField(mEditData.EvaluationHighRatio, GUILayout.Width(50));

        int tRoundPosition = 100;
        float tHighValue = 1 - mEditData.EvaluationHighRatio;
        EditorGUILayout.MinMaxSlider(ref mEditData.EvaluationLowRatio, ref tHighValue, 0, 1);
        mEditData.EvaluationLowRatio = Mathf.Round(mEditData.EvaluationLowRatio * tRoundPosition) / tRoundPosition;
        mEditData.EvaluationHighRatio = Mathf.Round((1 - tHighValue) * tRoundPosition) / tRoundPosition;
        mEditData.EvaluationMiddleRatio = Mathf.Round((tHighValue - mEditData.EvaluationLowRatio) * tRoundPosition) / tRoundPosition;
        GUILayout.EndHorizontal();


        GUILayout.BeginHorizontal();
        GUI.enabled = !(mCurrentTap == SequenceEditTap.Edit);
        if (GUILayout.Button("Edit"))
            mCurrentTap = SequenceEditTap.Edit;

        GUI.enabled = !(mCurrentTap == SequenceEditTap.Template);
        if (GUILayout.Button("Template"))
            mCurrentTap = SequenceEditTap.Template;

        GUI.enabled = true;
        GUILayout.EndHorizontal();

        //Sequence Data
        switch(mCurrentTap)
        {
            case SequenceEditTap.Edit:
                DrawSequenceEdit();
                break;
            case SequenceEditTap.Template:
                break;
        }
    }

    private void DrawCreateEditData()
    {
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Name : ", GUILayout.Width(50));
        mCreateDataName = EditorGUILayout.TextField(mCreateDataName, GUILayout.Width(200));
        GUI.enabled = !string.IsNullOrEmpty(mCreateDataName);
        if (GUILayout.Button("Create", GUILayout.Width(100)))
        {
            mEditData = CreateInstance<CStageData>();
            mEditData.StageName = mCreateDataName;
            AssetDatabase.CreateAsset(mEditData, string.Format(PATH_EDIT_DATA + "/{0}Data.asset", mCreateDataName));

            AssetDatabase.Refresh();
        }
        GUI.enabled = true;
        EditorGUILayout.LabelField("DataObject : ", GUILayout.Width(80));
        mEditData = EditorGUILayout.ObjectField(mEditData, typeof(CStageData), false) as CStageData;
        if (GUILayout.Button("Refrash", GUILayout.Width(80)))
        {
        }
        GUILayout.EndHorizontal();
    }
    private void DrawSequenceEdit()
    {
        EditorGUILayout.BeginHorizontal();
        mAddSeqBeat = CCustomField.FloatField("Beat : ", mAddSeqBeat, 40);
        if (GUILayout.Button("Add", GUILayout.Width(60)))
        {
            mEditData.SequenceList.Add(new CSequenceData(mAddSeqBeat));
        }
        if (GUILayout.Button("Append", GUILayout.Width(60)))
        {
            mAddSeqBeat = mEditData.SequenceList[mEditData.SequenceList.Count - 1].Beat + 1;
            mEditData.SequenceList.Add(new CSequenceData(mAddSeqBeat));
        }
        if (GUILayout.Button("Remove", GUILayout.Width(60)))
        {
            mEditData.SequenceList.RemoveAt(mEditData.SequenceList.Count - 1);
        }

        GUILayout.Space(8);
        GUILayout.Label("ActionCode : ", GUILayout.Width(80));
        mSelectedActionCodeIndex = EditorGUILayout.Popup(mSelectedActionCodeIndex, mEditData.ActionCodeList.ToArray(), GUILayout.Width(100));
        mAddActionCode = GUILayout.TextField(mAddActionCode, GUILayout.Width(100));
        if(GUILayout.Button("Add", GUILayout.Width(60)))
        {
            if(mEditData.ActionCodeList.Contains(mAddActionCode) == false)
            {
                mEditData.ActionCodeList.Add(mAddActionCode);
                mAddActionCode = string.Empty;
            }
        }
        if (GUILayout.Button("Remove", GUILayout.Width(60)))
        {
            mEditData.ActionCodeList.RemoveAt(mSelectedActionCodeIndex);
        }
        if (GUILayout.Button("Generate", GUILayout.Width(75)))
        {
            StringArrayToClass.Generate(mEditData.ActionCodeList.ToArray(), "C"+mEditData.StageName + "ActionCode", "Assets/Scripts/Stage/ActionCode");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        mCopySeqStartIndex = CCustomField.IntField("Index", mCopySeqStartIndex, 40);
        mCopySeqEndIndex = EditorGUILayout.IntField(mCopySeqEndIndex, GUILayout.Width(50));
        if (GUILayout.Button("Copy", GUILayout.Width(50)))
        {
            if(mCopySeqStartIndex < mCopySeqEndIndex &&
                mCopySeqEndIndex < mEditData.SequenceList.Count)
            {
                for (int i = mCopySeqStartIndex; i < mCopySeqEndIndex + 1; i++)
                {
                    var tCopy = mEditData.SequenceList[i].ToCopy(mEditData.SequenceList[mEditData.SequenceList.Count - 1].Beat + 1);
                    mEditData.SequenceList.Add(tCopy);
                }
            }
            else
            {
                Debug.Log("Index Range Error");
            }
        }
        EditorGUILayout.EndHorizontal();


        GUILayout.Box("Sequence List",GUILayout.Width(this.minSize.x * 0.59f));

        mSequenceScrollViewPos = GUILayout.BeginScrollView(mSequenceScrollViewPos);

        //int tMinIndex = 0;
        //int tMaxIndex = 40;
        //tMaxIndex = Mathf.Clamp(tMaxIndex, 0, mEditData.SequenceList.Count - 1);
        //for (int i = tMinIndex; i <= tMaxIndex; i++)
        for (int i = 0; i < mEditData.SequenceList.Count; i++)
        {
            if (mSeqIsExpanded.Count < i + 1)
            {
                mSeqIsExpanded.Add(false);
            }
            mSeqIsExpanded[i] = EditorGUILayout.Foldout(mSeqIsExpanded[i], string.Format("Beat {1} [{0}]", i, mEditData.SequenceList[i].Beat), true);
            if (mSeqIsExpanded[i])
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(17);
                mEditData.SequenceList[i].Beat = CCustomField.FloatField("Beat : ", mEditData.SequenceList[i].Beat, 40);
                mEditData.SequenceList[i].Input = (InputCode)CCustomField.EnumPopup("Input : ", mEditData.SequenceList[i].Input, 45, 100);

                string tActionCode = mEditData.SequenceList[i].ActionCode;
                int tActionCodeIndex = mEditData.ActionCodeList.IndexOf(tActionCode);
                if (tActionCodeIndex == -1)
                    tActionCodeIndex = 0;

                mEditData.SequenceList[i].ActionCode = mEditData.ActionCodeList[
                    CCustomField.Popup("ActionCode : ", tActionCodeIndex, mEditData.ActionCodeList.ToArray(), 85, 120)];
                EditorGUILayout.EndHorizontal();
            }

        }
        GUILayout.EndScrollView();


        //Rect tReorderRect = new Rect(0, EditorGUIUtility.singleLineHeight * 5, this.minSize.x - 30, this.minSize.y);
        //mReorderScrollViewPos = GUILayout.BeginScrollView(mReorderScrollViewPos);
        //mSeqReorderableList.DoLayoutList();
        //GUILayout.EndScrollView();

        //mStageDataSO.ApplyModifiedProperties();
    }
}

public static class CCustomField
{
    public static int IntField(string tTitle,int tValue,float tLabelWidth = 50,float tFieldWidth = 50)
    {
        EditorGUILayout.LabelField(tTitle, GUILayout.Width(tLabelWidth));
        return EditorGUILayout.IntField(tValue, GUILayout.Width(tFieldWidth));
    }
    public static float FloatField(string tTitle,float tValue, float tLabelWidth = 50, float tFieldWidth = 50)
    {
        EditorGUILayout.LabelField(tTitle, GUILayout.Width(tLabelWidth));
        return EditorGUILayout.FloatField(tValue, GUILayout.Width(tFieldWidth));
    }
    public static T ObjectField<T>(string tTitle, T tValue, float tLabelWidth = 50, float tFieldWidth = 50) where T: Object
    {
        EditorGUILayout.LabelField(tTitle, GUILayout.Width(tLabelWidth));
        return EditorGUILayout.ObjectField(tValue, typeof(T),false,GUILayout.Width(tFieldWidth)) as T;
    }
    public static System.Enum EnumPopup(string tTitle,System.Enum tValue, float tLabelWidth = 50, float tFieldWidth = 50)
    {
        EditorGUILayout.LabelField(tTitle, GUILayout.Width(tLabelWidth));
        return EditorGUILayout.EnumPopup(tValue,GUILayout.Width(tFieldWidth));
    }
    public static int Popup(string tTitle, int tSelected, string[] tDisplayOptions, float tLabelWidth = 50, float tFieldWidth = 50)
    {
        EditorGUILayout.LabelField(tTitle, GUILayout.Width(tLabelWidth));
        return EditorGUILayout.Popup(tSelected, tDisplayOptions, GUILayout.Width(tFieldWidth));
    }
}

public static class StringArrayToClass
{
    public static void Generate(string[] array, string name, string path, string nameSpace = null, string tag = null)
    {
        if (array == null || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(path))
            return;

        bool isNameSpace = !string.IsNullOrEmpty(nameSpace);

        StringBuilder enumString = new StringBuilder();

        if (isNameSpace)
        {
            enumString.Append("\t");
        }

        enumString.AppendFormat("public static class {0}\n", name);

        if (isNameSpace)
        {
            enumString.Append("\t");
        }

        enumString.Append("{");

        foreach (var str in array)
        {
            enumString.Append("\n\t");

            if (isNameSpace)
            {
                enumString.Append("\t");
            }

            if (string.IsNullOrEmpty(tag) == false)
                enumString.AppendFormat("{0}_", tag);

            //enumString.AppendFormat("{0},", str);
            enumString.AppendFormat("public const string {0} = \"{1}\";", str.ToUpper(), str);

        }

        enumString.AppendLine();

        if (isNameSpace)
        {
            enumString.Append("\t");
        }

        enumString.Append("}");

        string result = enumString.ToString();
        enumString.Remove(0, enumString.Length);
        //Debug.Log(result);

        if (isNameSpace)
        {
            result = enumString
                .AppendFormat("namespace {0}\n", nameSpace)
                .Append("{")
                .AppendLine()
                .AppendFormat("{0}", result)
                .AppendLine()
                .Append("}")
                .ToString();
        }


        path = string.Format("{0}/{1}/{2}.cs",
            System.Environment.CurrentDirectory.Replace('\\', '/'),
            path,
            name);

        //Debug.Log(result);
        //Debug.Log(path);
        using (StreamWriter sw = new StreamWriter(path))
        {
            sw.Write(result);
        }
        AssetDatabase.Refresh();
    }
}