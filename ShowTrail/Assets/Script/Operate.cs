using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class Operate : MonoBehaviour
{

    public Camera camera;
    public GameObject tag;
    public GUIText txtError;
    public static GUIText txtShowError;
    public GUIText txtResult;
    public GUIText txtProbability;
    public Transform tagParent;
    public TrailRenderer trail;


    public static MouseState mouseMode = MouseState.LEAVE;


    //程序运行的模式
    public static RUN_MODE mode = RUN_MODE.UNREADY;

    //手势相关数据对象
    private GestureData data;

    //神经网络对象
    private NeuralNet net;

    //手势数目
    private int numValidPatterns;

    //需要记录的鼠标点数
    private int numSmoothPoints;

    //用户鼠标输入的手势向量
    private List<Vector3> rawPath;

    //光滑化之后的手势向量
    private List<Vector3> smoothPath;

    //待匹配的向量
    private List<double> vectors;

    //网络最大的输出（最像的匹配）
    private double highestOutput;

    //网络最大的输出对应的手势
    private int bestMatch;

    //匹配的手势
    private int match;

    void Start()
    {
        txtShowError = txtError;
        numValidPatterns = Useful.NUM_PATTERNS;
        numSmoothPoints = Useful.NUM_VECTORS + 1;
        rawPath = new List<Vector3>();
        smoothPath = new List<Vector3>();
        vectors = new List<double>();
        highestOutput = 0.0;
        bestMatch = -1;
        match = -1;
        data = new GestureData(Useful.NUM_PATTERNS, Useful.NUM_VECTORS);
        net = new NeuralNet(Useful.NUM_VECTORS * 2, numValidPatterns);
    }

	void Update () {

        txtError.text = "误差率：" + ((float)net.errorSum).ToString();
        if (Input.GetKeyDown(KeyCode.S))
        {
            mode = RUN_MODE.SAVING;
            isShow = true;
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            mode = RUN_MODE.LEARNING;
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            //Thread oThread = new Thread(new ThreadStart(StartTrainNetWork));
            //oThread.Start();
            StartTrainNetWork();
        }

        if (Input.GetMouseButton(0))
        {
            Record();
        }
        else 
        {
            mouseMode = MouseState.LEAVE;
        }

        if (Input.GetMouseButtonDown(0)) {
            ClearRecord();
        }
        if (Input.GetMouseButtonUp(0)) {
            if (mode == RUN_MODE.LEARNING)
            {
                if (Smooth()) {
                    ShowSmoothPoint();
                    CreateVectors();
                }
                isShow = true;
            }
            else
            {
                StartMatch();
            }
        }

	}

    private string strName = "";
    private bool isShow = false;
    void OnGUI()
    {
        if (isShow)
        {
            GUI.Label(new Rect(0, 0, 100, 50), "手势名称：");
            strName = GUI.TextField(new Rect(60, 0, 100, 30), strName);
            if (GUI.Button(new Rect(160, 0, 50, 30), "确定"))
            {

                StartLearn();
                isShow = false;
                ClearRecord();
            }
        }
    }

    private void StartTrainNetWork() {
        TrainNetwork();
    }

    //记录鼠标向量
    void Record() {
        mouseMode = MouseState.PRESS;
        if (Operate.mode == RUN_MODE.ACTIVE || Operate.mode == RUN_MODE.LEARNING)
        {
            Vector3 v = Input.mousePosition;
            v.z = 10;
            v = camera.ScreenToWorldPoint(v);
            transform.position = v;
            rawPath.Add(camera.transform.InverseTransformPoint(v));
        }
    }

    //对鼠标手势向量光滑化，便于模式匹配
    private bool Smooth()
    {
        //确保包含计算所需的足够的点
        if (rawPath.Count < Useful.NUM_VECTORS + 1)
        {
            return false;
        }

        smoothPath = new List<Vector3>(rawPath);

        //对所有的最小跨度点对取中点，删除原来的点，循环执行
        while (smoothPath.Count > Useful.NUM_VECTORS + 1)
        {
            double ShortestSoFar = double.MaxValue;
            int PointMarker = 0;

            //计算最小跨度
            for (int spanFront = 2; spanFront < smoothPath.Count - 1; spanFront++)
            {
                //计算点对距离
                double length = Vector3.Distance(smoothPath[spanFront - 1], smoothPath[spanFront]);

                if (length < ShortestSoFar)
                {
                    ShortestSoFar = length;
                    PointMarker = spanFront;
                }
            }

            //插入中点，删除原来的点
            Vector3 newPoint = new Vector3(0,0,10);
            newPoint.x = (smoothPath[PointMarker - 1].x + smoothPath[PointMarker].x) / 2;
            newPoint.y = (smoothPath[PointMarker - 1].y + smoothPath[PointMarker].y) / 2;
            smoothPath[PointMarker - 1] = newPoint;
            smoothPath.RemoveAt(PointMarker);
        }
        return true;
    }


    //显示平滑后的点
    void ShowSmoothPoint() {
        tagInsParent = Instantiate(tagParent, Vector3.zero, Quaternion.identity) as Transform;
        for (int i = 0; i < smoothPath.Count; i++)
        {
            GameObject o = Instantiate(tag, smoothPath[i], Quaternion.identity) as GameObject;
            o.transform.parent = tagInsParent;
        }
    }

    //对鼠标手势向量归一化，生成待匹配的向量
    private void CreateVectors()
    {
        for (int p = 1; p < smoothPath.Count; ++p)
        {
            Vector3 v = smoothPath[p] - smoothPath[p - 1];
            v = v.normalized;
            vectors.Add(v.x);
            vectors.Add(v.y);
        }
    }

    //模式匹配
    private bool TestForMatch()
    {
        List<double> outputs = net.Update(vectors);
        if (outputs.Count == 0)
        {
            return false;
        }

        highestOutput = 0;
        bestMatch = 0;
        match = -1;

        for (int i = 0; i < outputs.Count; i++)
        {
            if (outputs[i] > highestOutput)
            {
                //记录最像的匹配
                highestOutput = outputs[i];
                bestMatch = i;

                //确定是这个手势
                if (highestOutput > Useful.MATCH_TOLERANCE)
                {
                    match = bestMatch;
                }
            }
        }
        return true;
    }

    //显示匹配结果
    private void ShowResult()
    {
        print(data.PatternName(bestMatch));
        txtResult.text = "名字："+data.PatternName(bestMatch).ToString();
        txtProbability.text = "匹配率："+highestOutput.ToString();
        int pct = (int)(highestOutput * 10.0);
        if (pct > 9)
        {
            pct = 9;
        }
    }

    //训练神经网络
    private bool TrainNetwork()
    {
        mode = RUN_MODE.TRAINING;
        if (!(net.Train(data)))
        {
            return false;
        }

        mode = RUN_MODE.ACTIVE;
        return true;
    }

    //使用新的参数重建神经网络
    private void RenewNetwork()
    {
        net = new NeuralNet(Useful.NUM_VECTORS * 2, numValidPatterns);
        //Thread oThread = new Thread(new ThreadStart(StartTrainNetWork));
        StartTrainNetWork();
    }

    //开始手势识别
    private bool StartMatch()
    {
        if (Smooth())
        {
            ShowSmoothPoint();
            CreateVectors();

            //识别
            if (mode == RUN_MODE.ACTIVE)
            {
                if (!TestForMatch())
                {
                    return false;
                }
                else
                {
                    ShowResult();
                }
            }
            //学习
            //else if (mode == RUN_MODE.LEARNING)
            //{
            //    //保存新的手势并重新训练网络
            //    data.AddPattern(vectors, strName);
            //    numValidPatterns++;
            //    net = new NeuralNet(Useful.NUM_VECTORS * 2, numValidPatterns);
            //    RenewNetwork();
            //    mode = RUN_MODE.ACTIVE;
            //}
        }
        else
        {
            ClearRecord();
        }

        return true;
    }

    //学习新的手势
    private void StartLearn() {
        if (mode == RUN_MODE.LEARNING)
        {
            //保存新的手势并重新训练网络
            print(vectors.Count);
            data.AddPattern(vectors, strName);
            numValidPatterns++;
            net = new NeuralNet(Useful.NUM_VECTORS * 2, numValidPatterns);
            RenewNetwork();
            mode = RUN_MODE.ACTIVE;
        }
    }

    private Transform tagInsParent;
    void ClearRecord() {
        if (!isShow)
        {
            rawPath.Clear();
            smoothPath.Clear();
            vectors.Clear();
            if (tagInsParent != null)
            {
                Destroy(tagInsParent.gameObject);
            }
        }
    }
}

public enum MouseState { PRESS, LEAVE };