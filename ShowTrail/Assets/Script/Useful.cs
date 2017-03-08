using UnityEngine;
using System.Collections;

/*
 这个类保存一些配置信息
 */
public static class Useful{

    //内置的手势模式的个数
    public const int NUM_PATTERNS = 4;

    //手势向量的长度(坐标个数)
    public const int NUM_VECTORS = 24;

    //当输出超过下面数值时就认为是100%匹配
    public const double MATCH_TOLERANCE = 0.96;

    //神经细胞的参数
    public const double ACTIVATION_RESPONSE = 1.0; //用于控制Sigmoid函数曲线陡曲的快慢的参数，此参数值越大曲线越平滑（此函数的输出区间为(0,1)）
    public const double BIAS = 1.0;                //偏移值（阀值）设置
    public static double ERROR_THRESHOLD = 0.003;   //可容忍的误差极限
    static public double LEARNING_RATE = 0.5;      //学习率
    public static int NUM_HIDDEN_NEURONS = 6;      //隐藏层的神经细胞个数
    public static bool WITH_MOMENTUM = false;      //是否使用动量
    public static double MOMENTUM = 0.9;
    public static bool WITH_NOISE = false;         //是否噪声注入
    public static double MAX_NOISE_TO_ADD = 0.1;
    public static int NUM_HIDDEN_LAYERS = 1;       //隐含层数

    //预定义的手势名字
    public static string[] initNames = 
    {
        "向上",
        "向下",
        "向左",
        "向右"
    };

    //预定义的手势向量
    private static float[] G01 = { 0,1, 0,1, 0,1, 0,1, 0,1, 0,1, 0,1, 0,1, 0,1, 0,1, 0,1, 0,1,              //向上
                                 0,1, 0,1, 0,1, 0,1, 0,1, 0,1, 0,1, 0,1, 0,1, 0,1, 0,1, 0,1, };             
    private static float[] G02 = { 0,-1, 0,-1, 0,-1, 0,-1, 0,-1, 0,-1, 0,-1, 0,-1, 0,-1, 0,-1, 0,-1, 0,-1,  //向下
                                 0,-1, 0,-1, 0,-1, 0,-1, 0,-1, 0,-1, 0,-1, 0,-1, 0,-1, 0,-1, 0,-1, 0,-1, }; 
    private static float[] G03 = { -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, -1,0,  //向左
                                 -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, };
    private static float[] G04 = {1,0, 1,0, 1,0, 1,0, 1,0, 1,0, 1,0, 1,0, 1,0, 1,0, 1,0, 1,0,               //向右    
                                 1,0, 1,0, 1,0, 1,0, 1,0, 1,0, 1,0, 1,0, 1,0, 1,0, 1,0, 1,0, };

    public static float[][] initPatterns = { G01, G02, G03, G04 };

    //返回-1到1之间的随机数
    public static double RandomClamped() {
        return Random.Range(-1, 1);
    }

}

//程序的运行状态
public enum RUN_MODE { LEARNING, ACTIVE, UNREADY, TRAINING, SAVING };
