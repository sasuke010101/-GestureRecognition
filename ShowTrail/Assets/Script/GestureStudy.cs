using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 此类用于存储鼠标手势相关数据
 */
public class GestureData{

    //手势名称列表
    private List<string> names;

    //手势向量列表
    private List<List<double>> patterns;

    //加载的手势数目
    private int patternNum;

    //手势向量长度
    private int patternSize;

    //训练集
    public List<List<double>> setIn;
    public List<List<double>> setOut;

    //构造函数
    public GestureData(int _patternNum, int _patternSize)
    {
        names = new List<string>();
        patterns = new List<List<double>>();
        setIn = new List<List<double>>();
        setOut = new List<List<double>>();
        patternNum = _patternNum;
        patternSize = _patternSize;

        InitPattern();//初始化预定义手势
        CreateTrainingSet();//创建训练集
    }

    //初始化预定义手势
    public void InitPattern() {
        for (int i = 0; i < patternNum; i++) {
            List<double> temp = new List<double>();
            for (int j = 0; j < patternSize * 2; j++) {
                temp.Add(Useful.initPatterns[i][j]);//把每个手势的向量依次加入临时列表
            }
            patterns.Add(temp);
            names.Add(Useful.initNames[i]);//把每个手势的名字加入名称列表，注意和向量列表一一对应
        }
    }

    //增加新的手势
    public bool AddPattern(List<double> _pattern, string _name)
    {
        if (_pattern.Count != patternSize * 2 || _pattern.Count == 0)
        {
            throw new System.Exception("手势向量长度错误");
        }
        patterns.Add(new List<double>(_pattern)); //注：原版这里使用的是复制_pattern列表(不用复制在第二次学习时出现死机)。
        names.Add(_name);       //因为List是引用类型，手势模版则一直覆盖掉以前的，而手势数目却在不断增加。
        patternNum++;//手势数目自加1

        CreateTrainingSet();
        return true;
    }

    //创建用于训练的输入和输出集
    public void CreateTrainingSet()
    {
        setIn.Clear();
        setOut.Clear();

        for (int i = 0; i < patternNum; i++) 
        {
            setIn.Add(patterns[i]);

            //相关的输出为1，不相关的输出为0
            List<double> output = new List<double>();
            for (int j = 0; j < patternNum; j++) 
            {
                output.Add(0);
            }
            output[i] = 1;

            setOut.Add(output);
        }
    }

    //获得手势的名字
    public string PatternName(int index) 
    {
        if (index != null)
        {
            return names[index];
        }
        else 
        {
            return "";
        }
    }

}
