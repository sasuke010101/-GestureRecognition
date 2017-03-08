using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

/*
 定义神经网络
 */
public class NeuralNet{

    //输入数
    private int numInputs;

    //输出数
    private int numOutputs;

    //隐含层数
    private int numHiddenLayers;

    //每个隐含层的神经元数
    private int neuronsPerHiddenLyr;

    //学习率
    private double learningRate;

    //积累错误
    public double errorSum;

    //是否经过了训练
    public bool trained;

    //迭代数
    public int numEpochs;

    //神经网络的各个层
    public List<NeuronLayer> layers;


    //构造函数
    public NeuralNet(int _numInputs,int _numOutputs) {
        numInputs = _numInputs;
        numOutputs = _numOutputs;
        numHiddenLayers = Useful.NUM_HIDDEN_LAYERS;
        neuronsPerHiddenLyr = Useful.NUM_HIDDEN_NEURONS;
        learningRate = Useful.LEARNING_RATE;
        errorSum = 9999;
        trained = false;
        numEpochs = 0;
        layers = new List<NeuronLayer>();
        CreateNet();
    }

    //创建网络
    public void CreateNet() {
        if (numHiddenLayers > 0)
        {
            //创建第一个隐藏层
            layers.Add(new NeuronLayer(neuronsPerHiddenLyr, numInputs));
            for (int i = 0; i < numHiddenLayers - 1; i++)
            {
                layers.Add(new NeuronLayer(neuronsPerHiddenLyr, neuronsPerHiddenLyr));
            }

            //输出层
            layers.Add(new NeuronLayer(numOutputs, neuronsPerHiddenLyr));
        }
        else
        {
            //输出层
            layers.Add(new NeuronLayer(numOutputs, numInputs));
        }
    }

    //Sigmoid激励函数
    public double Sigmoid(double a,double p) 
    {
        return (1.0 / (1.0 + System.Math.Exp(-a / p)));
    }

    //训练神经网络
    public bool Train(GestureData data)
    {
        List<List<double>> SetIn = new List<List<double>>(data.setIn);
        List<List<double>> SetOut = new List<List<double>>(data.setOut);

        //校验训练集
        if ((SetIn.Count != SetOut.Count) || (SetIn[0].Count != numInputs) || (SetOut[0].Count != numOutputs))
        {
            throw new System.Exception("训练集输入输出不符！");
        }

        InitializeNetwork();

        //训练直至错误小于阈值
        while (errorSum > Useful.ERROR_THRESHOLD)
        {
            //迭代训练
            if (!NetworkTrainingEpoch(SetIn, SetOut))
            {
                return false;
            }
            numEpochs++;
        }

        trained = true;
        return true;
    }

    //将所有权重设置为随机的小值
    private void InitializeNetwork()
    {
        //对于每一层执行
        for (int i = 0; i < numHiddenLayers + 1; i++)
        {
            //对于每个神经元执行
            for (int n = 0; n < layers[i].numNeurons; n++)
            {
                //对于每个权重执行
                for (int k = 0; k < layers[i].neurons[n].numInputs; k++)
                {
                    layers[i].neurons[n].weights[k] = Useful.RandomClamped();
                }
            }
        }

        errorSum = 9999;
        numEpochs = 0;
    }

    //训练神经网络的迭代
    private bool NetworkTrainingEpoch(List<List<double>> SetIn, List<List<double>> SetOut)
    {
        if (Useful.WITH_MOMENTUM)//添加动量
        {
            return NetworkTrainingEpochWithMomentum(SetIn, SetOut);
        }
        else//不添加动量
        {
            return NetworkTrainingEpochNonMomentum(SetIn, SetOut);
        }
    }

    //训练神经网络的迭代（无动量）
    private bool NetworkTrainingEpochNonMomentum(List<List<double>> SetIn, List<List<double>> SetOut)
    {

        int curWeight;
        int curNrnOut, curNrnHid;

        errorSum = 0;

        //计算积累错误，修正权重
        for (int vec = 0; vec < SetIn.Count; vec++)
        {

            List<double> outputs = Update(SetIn[vec]);

            if (outputs.Count == 0)
            {
                return false;
            }

            //修正权重
            for (int op = 0; op < numOutputs; op++)
            {
                //计算偏差
                double err = (SetOut[vec][op] - outputs[op]) * outputs[op] * (1.0 - outputs[op]);
                layers[1].neurons[op].error = err;

                errorSum += (SetOut[vec][op] - outputs[op]) * (SetOut[vec][op] - outputs[op]);
                
                curWeight = 0;
                curNrnHid = 0;

                //除bias之外的权值
                while (curWeight < layers[1].neurons[op].weights.Count - 1)
                {
                    //新权值
                    layers[1].neurons[op].weights[curWeight] += err * learningRate * layers[0].neurons[curNrnHid].activation;
                    ++curWeight; ++curNrnHid;
                }

                //bias(偏移值也就是阀值)
                layers[1].neurons[op].weights[curWeight] += err * learningRate * Useful.BIAS;
            }

            curNrnHid = 0;

            int n = 0;

            //对于隐含层的每个神经元计算
            while (curNrnHid < layers[0].neurons.Count)
            {
                double err = 0;

                curNrnOut = 0;

                //对于输出层的每个神经元计算
                while (curNrnOut < layers[1].neurons.Count)
                {
                    err += layers[1].neurons[curNrnOut].error * layers[1].neurons[curNrnOut].weights[n];
                    ++curNrnOut;
                }

                //计算偏差
                err *= layers[0].neurons[curNrnHid].activation * (1.0 - layers[0].neurons[curNrnHid].activation);

                //计算新权重
                for (int w = 0; w < numInputs; w++)
                {
                    //BP
                    layers[0].neurons[curNrnHid].weights[w] += err * learningRate * SetIn[vec][w];
                }

                //bias(添加偏移值)
                layers[0].neurons[curNrnHid].weights[numInputs] += err * learningRate * Useful.BIAS;

                ++curNrnHid;
                ++n;
            }
        }

        return true;
    }

    //训练神经网络的迭代（增加动量）
    private bool NetworkTrainingEpochWithMomentum(List<List<double>> SetIn, List<List<double>> SetOut)
    {

        int curWeight;
        int curNrnOut, curNrnHid;

        double weightUpdate = 0;

        errorSum = 0;

        //计算积累错误，修正权重
        for (int vec = 0; vec < SetIn.Count; vec++)
        {
            List<double> outputs = Update(SetIn[vec]);

            if (outputs.Count == 0)
            {
                return false;
            }

            //修正权重
            for (int op = 0; op < numOutputs; op++)
            {
                //计算偏差
                double err = (SetOut[vec][op] - outputs[op]) * outputs[op] * (1.0 - outputs[op]);
                layers[1].neurons[op].error = err;

                errorSum += (SetOut[vec][op] - outputs[op]) * (SetOut[vec][op] - outputs[op]);

                curWeight = 0;
                curNrnHid = 0;

                int w = 0;

                //除bias之外的权值
                while (curWeight < layers[1].neurons[op].weights.Count - 1)
                {
                    //计算权重更新
                    weightUpdate = err * learningRate * layers[0].neurons[curNrnHid].activation;
                    //加入动量之后的新权重
                    layers[1].neurons[op].weights[curWeight] += weightUpdate + layers[1].neurons[op].prevUpdate[w] * Useful.MOMENTUM;
                    //记录权重更新
                    layers[1].neurons[op].prevUpdate[w] = weightUpdate;

                    ++curWeight; ++curNrnHid; ++w;
                }

                //bias
                weightUpdate = err * learningRate * Useful.BIAS;
                layers[1].neurons[op].weights[curWeight] += weightUpdate + layers[1].neurons[op].prevUpdate[w] * Useful.MOMENTUM;
                layers[1].neurons[op].prevUpdate[w] = weightUpdate;

            }

            curNrnHid = 0;

            int n = 0;

            //对于隐含层的每个神经元计算
            while (curNrnHid < layers[0].neurons.Count)
            {
                double err = 0;

                curNrnOut = 0;

                //对于输出层的每个神经元计算
                while (curNrnOut < layers[1].neurons.Count)
                {
                    err += layers[1].neurons[curNrnOut].error * layers[1].neurons[curNrnOut].weights[n];
                    ++curNrnOut;
                }

                //计算偏差
                err *= layers[0].neurons[curNrnHid].activation * (1.0 - layers[0].neurons[curNrnHid].activation);

                //计算新权重
                int w;
                for (w = 0; w < numInputs; w++)
                {
                    //BP
                    weightUpdate = err * learningRate * SetIn[vec][w];
                    layers[0].neurons[curNrnHid].weights[w] += weightUpdate + layers[0].neurons[curNrnHid].prevUpdate[w] * Useful.MOMENTUM;
                    layers[0].neurons[curNrnHid].prevUpdate[w] = weightUpdate;
                }

                //bias
                weightUpdate = err * learningRate * Useful.BIAS;
                layers[0].neurons[curNrnHid].weights[numInputs] += weightUpdate + layers[0].neurons[curNrnHid].prevUpdate[w] * Useful.MOMENTUM;
                layers[0].neurons[curNrnHid].prevUpdate[w] = weightUpdate;

                ++curNrnHid;
                ++n;
            }
        }
        return true;
    }

    //计算网络输出
    public List<double> Update(List<double> _inputs)
    {
        List<double> inputs = new List<double>(_inputs);
        List<double> outputs = new List<double>();
        int cWeight = 0;

        //添加噪声
        if (Useful.WITH_NOISE)
        {
            for (int k = 0; k < inputs.Count; k++)
            {
                inputs[k] += Useful.RandomClamped() * Useful.MAX_NOISE_TO_ADD;
            }
        }

        //验证输入长度
        if (inputs.Count != numInputs)
        {
            return outputs;
        }

        //对于每一层执行
        for (int i = 0; i < numHiddenLayers + 1; i++)
        {
            if (i > 0)
            {
                inputs = new List<double>(outputs);
            }
            outputs.Clear();

            cWeight = 0;

            //对于每个神经元执行
            for (int n = 0; n < layers[i].numNeurons; n++)
            {
                double netinput = 0;

                int num = layers[i].neurons[n].numInputs;

                //对于每个权重执行
                for (int k = 0; k < num - 1; k++)
                {
                    netinput += layers[i].neurons[n].weights[k] * inputs[cWeight++];
                }

                netinput += layers[i].neurons[n].weights[num - 1] * Useful.BIAS;//加上偏移值(阀值)

                layers[i].neurons[n].activation = Sigmoid(netinput, Useful.ACTIVATION_RESPONSE);

                outputs.Add(layers[i].neurons[n].activation);

                cWeight = 0;
            }
        }

        return outputs;
    }
}

//神经元
public class Neuron {

    //神经元输入数
    public int numInputs;

    //权值向量
    public List<double> weights;

    //前一步的权值更新向量(用于含有动量的网络权重修正)
    public List<double> prevUpdate;

    //活跃值
    public double activation;

    //错误值
    public double error;

    //构造函数
    public Neuron(int _numInputs)
    {
        numInputs = _numInputs + 1;
        activation = 0;
        error = 0;
        weights = new List<double>();
        prevUpdate = new List<double>();

        //生成随机权重
        for (int i = 0; i < numInputs; i++)
        {
            weights.Add(Useful.RandomClamped());
            prevUpdate.Add(0.0);
        }
    }
}

//神经网络层
public class NeuronLayer
{
    //本层神经元数
    public int numNeurons;

    //神经元
    public List<Neuron> neurons;

    //构造函数
    public NeuronLayer(int _numNeurons, int _numInputsPerNeuron)
    {
        numNeurons = _numNeurons;
        neurons = new List<Neuron>();

        for (int i = 0; i < numNeurons; i++)
        {
            neurons.Add(new Neuron(_numInputsPerNeuron));
        }
    }

}

