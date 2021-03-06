﻿using Microsoft.ML;
using Microsoft.ML.Model;
using Microsoft.ML.Trainers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML.NET.Demo
{
    public class Compute
    {
        public void Excute()
        {

            // 创建ML.NET执行上下文
            var mlContext = new MLContext();

            // 初始化训练算法
            var trainers = new List<ITrainerEstimator<ISingleFeaturePredictionTransformer<ModelParametersBase<float>>, ModelParametersBase<float>>>()
            {
                mlContext.Regression.Trainers.FastTree(),
                mlContext.Regression.Trainers.FastForest(),
                mlContext.Regression.Trainers.FastTreeTweedie(),
                mlContext.Regression.Trainers.GeneralizedAdditiveModels(),
                mlContext.Regression.Trainers.OnlineGradientDescent(),
                mlContext.Regression.Trainers.PoissonRegression(),
                mlContext.Regression.Trainers.StochasticDualCoordinateAscent()
            }; 

            // 创建训练任务
            var session = new LearningSession(mlContext, trainers);

            // 读入训练数据集以及测试数据集 


            var trainingDataView = session.LoadDataView("Data/BaseData.txt");
            var testingDataView = session.LoadDataView("Data/PBaseData.txt");


            

            Console.WriteLine(">>> 开始测试并评估...");
            Console.WriteLine();


            // 基于训练数据集进行训练，并基于测试数据集进行评估，然后输出评估结果
            var regressionMetrics = session.TrainAndEvaluate(trainingDataView, testingDataView);
            foreach (var item in regressionMetrics)
            {
                LearningSession.OutputRegressionMetrics(item.Key, item.Value);
            }

            // 找到RMS最小的算法，作为最优算法
            var winnerAlgorithmName = regressionMetrics.OrderBy(x => x.Value.Rms).First().Key;
            Console.WriteLine($"最优算法为：{winnerAlgorithmName}");
            Console.WriteLine();


            // 使用最优算法进行预测
            Console.WriteLine("以下是基于测试样本数据的预测结果");
            Console.WriteLine("==============================");
            var winnerModel = session.GetTrainedModel(winnerAlgorithmName);
            var samples = ReadPredictionSamples().ToList();
            foreach (var sample in samples)
            {
                var prediction = session.Predict(winnerModel, sample);
                Console.WriteLine($"测试样本G2: {sample.WearPoint}，预测值：{prediction.PWear}");
            }
            Console.WriteLine();


        }

        static IEnumerable<BaseModel> ReadPredictionSamples()
        {
            object ConvertValue(string val, Type toType)
            {
                if (toType == typeof(float))
                {
                    return Convert.ToSingle(val);
                }

                return val;
            }

            var predictionSamples = new List<BaseModel>();
            using (var fileStream = new FileStream("Data/PBaseData.txt", FileMode.Open, FileAccess.Read))
            {
                using (var textReader = new StreamReader(fileStream))
                {
                    var lineNumber = 0;
                    var line = string.Empty;
                    var columns = new List<string>();
                    while (!textReader.EndOfStream)
                    {
                        line = textReader.ReadLine();
                        if (lineNumber == 0)
                        {
                            columns.AddRange(line.Split('\t'));
                        }
                        else
                        {
                            var values = line.Split('\t');
                            var sample = new BaseModel();
                            for (var idx = 0; idx < values.Length; idx++)
                            {
                                var column = columns[idx];
                                var fieldInfo = typeof(BaseModel)
                                    .GetFields()
                                    .FirstOrDefault(x => string.Equals(x.Name, column, StringComparison.InvariantCultureIgnoreCase));

                                if (fieldInfo != null)
                                {
                                    var cc = values[idx];

                                    var bb = ConvertValue(cc, fieldInfo.FieldType);

                                    fieldInfo.SetValue(sample,bb);
                                }
                            }

                            predictionSamples.Add(sample);
                        }
                        lineNumber++;
                    }
                }
            }

            return predictionSamples;
        }
    }
}
