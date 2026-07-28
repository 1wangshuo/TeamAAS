using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamAAS.Robot.Core
{
    /// <summary>
    /// 三点建立坐标系(或者原点及角度)，并实现Local与Word坐标系直接的坐标互转换
    /// </summary>
    public class CoordinateTransformer
    {
        /// <summary>
        /// 三个点建立坐标系时确定是以X轴作为基准还是Y轴作为基准
        /// </summary>
        public enum Axis { X, Y };
        /// <summary>
        /// P1 作为新坐标系的原点
        /// </summary>
        public Vector<double> P1 { get; }
        /// <summary>
        /// 坐标系的基向量
        /// </summary>
        public Vector<double> E1 { get; }
        /// <summary>
        /// 坐标系的基向量
        /// </summary>
        public Vector<double> E2 { get; }
        public Matrix<double> R { get; }
        /// <summary>
        /// 旋转矩阵
        /// </summary>
        public Matrix<double> InvR { get; }
        /// <summary>
        /// 平移矩阵
        /// </summary>
        public Matrix<double> T { get; }

        /// <summary>
        /// 坐标系的旋转角度
        /// </summary>
        public double Angle
        {
            get
            {

                //Vector2 e1 = new Vector2(Math.Cos(radians), Math.Sin(radians)); // 新x轴向量
                //Vector2 e2 = new Vector2(-Math.Sin(radians), Math.Cos(radians)); // 新y轴向量
                double thetaRand1 = Math.Asin(E1[1]);
                double thetaDeg1 = thetaRand1 * 180 / Math.PI; // 将弧度值转换为角度值
                return Math.Round(thetaDeg1, 3);
            }
        }

        /// <summary>
        /// 三点建立坐标系，并实现Local与Word坐标系直接的坐标互转换
        /// </summary>
        /// <param name="x1">原点X坐标</param>
        /// <param name="y1">原点Y坐标</param>
        /// <param name="x2">X轴上的点X坐标</param>
        /// <param name="y2">X轴上的点Y坐标</param>
        /// <param name="x3">Y轴上的点X坐标</param>
        /// <param name="y3">Y轴上的点Y坐标</param>
        /// <param name="axis">三个点建立坐标系时确定是以X轴作为基准还是Y轴作为基准</param>
        public CoordinateTransformer(double x1, double y1, double x2, double y2, double x3, double y3, Axis axis = Axis.X)
        {
            //假设我们有三个点 P1(x1, y1), P2(x2, y2), P3(x3, y3)，它们是笛卡尔坐标系的三个点。我们要基于这三个点创建一个经过旋转和平移的坐标系，并且要求两个坐标系中的点可以互相转换。
            //1.新坐标系的原点：我们可以将 P1 作为新坐标系的原点。因此，新坐标系的原点坐标为 (x1, y1)。
            P1 = Vector<double>.Build.Dense(new double[] { x1, y1 });

            // Calculate basis vectors
            Vector<double> p2 = Vector<double>.Build.Dense(new double[] { x2, y2 });
            Vector<double> p3 = Vector<double>.Build.Dense(new double[] { x3, y3 });

            //2.计算坐标系的基向量：
            if (axis == Axis.X)
            {
                E1 = (p2 - P1) / (p2 - P1).L2Norm();
                E2 = (p3 - P1 - E1.DotProduct(p3 - P1) * E1) / (p3 - P1 - E1.DotProduct(p3 - P1) * E1).L2Norm();
            }
            else
            {
                E2 = (p3 - P1) / (p3 - P1).L2Norm();
                E1 = (p2 - P1 - E2.DotProduct(p2 - P1) * E2) / (p2 - P1 - E2.DotProduct(p2 - P1) * E2).L2Norm();
            }

            //3.计算旋转矩阵：我们可以使用基向量组成的矩阵作为旋转矩阵。具体地，我们可以将基向量 e1 和 e2 分别作为新坐标系下的 $x$ 和 $y$ 轴，然后组成一个矩阵，再对这个矩阵求逆即可得到旋转矩阵。代码实现：
            // Compute rotation matrix
            R = Matrix<double>.Build.DenseOfColumnVectors(E1, E2);
            InvR = R.Inverse();

            //4.平移矩阵：由于我们选择的原点是 P1，因此我们需要对所有点进行平移，使得新坐标系的原点与笛卡尔坐标系的原点重合。具体地，我们可以使用以下平移矩阵：
            // Compute translation matrix
            T = Matrix<double>.Build.DenseIdentity(3);
            T[0, 2] = -P1[0];
            T[1, 2] = -P1[1];
        }

        /// <summary>
        /// 三点建立坐标系，并实现Local与Word坐标系直接的坐标互转换
        /// </summary>
        /// <param name="Po">原点坐标</param>
        /// <param name="Px">X轴上的点坐标</param>
        /// <param name="Py">Y轴上的点坐标</param>
        /// <param name="axis">三个点建立坐标系时确定是以X轴作为基准还是Y轴作为基准</param>
        public CoordinateTransformer(PointF Po, PointF Px, PointF Py, Axis axis = Axis.X)
        {
            //假设我们有三个点 P1(x1, y1), P2(x2, y2), P3(x3, y3)，它们是笛卡尔坐标系的三个点。我们要基于这三个点创建一个经过旋转和平移的坐标系，并且要求两个坐标系中的点可以互相转换。
            //1.新坐标系的原点：我们可以将 P1 作为新坐标系的原点。因此，新坐标系的原点坐标为 (x1, y1)。
            P1 = Vector<double>.Build.Dense(new double[] { Po.X, Po.Y });

            // Calculate basis vectors
            Vector<double> p2 = Vector<double>.Build.Dense(new double[] { Px.X, Px.Y });
            Vector<double> p3 = Vector<double>.Build.Dense(new double[] { Py.X, Py.Y });

            //2.计算坐标系的基向量：
            if (axis == Axis.X)
            {
                E1 = (p2 - P1) / (p2 - P1).L2Norm();
                E2 = (p3 - P1 - E1.DotProduct(p3 - P1) * E1) / (p3 - P1 - E1.DotProduct(p3 - P1) * E1).L2Norm();
            }
            else
            {
                E2 = (p3 - P1) / (p3 - P1).L2Norm();
                E1 = (p2 - P1 - E2.DotProduct(p2 - P1) * E2) / (p2 - P1 - E2.DotProduct(p2 - P1) * E2).L2Norm();
            }

            //3.计算旋转矩阵：我们可以使用基向量组成的矩阵作为旋转矩阵。具体地，我们可以将基向量 e1 和 e2 分别作为新坐标系下的 $x$ 和 $y$ 轴，然后组成一个矩阵，再对这个矩阵求逆即可得到旋转矩阵。代码实现：
            // Compute rotation matrix
            R = Matrix<double>.Build.DenseOfColumnVectors(E1, E2);
            InvR = R.Inverse();

            //4.平移矩阵：由于我们选择的原点是 P1，因此我们需要对所有点进行平移，使得新坐标系的原点与笛卡尔坐标系的原点重合。具体地，我们可以使用以下平移矩阵：
            // Compute translation matrix
            T = Matrix<double>.Build.DenseIdentity(3);
            T[0, 2] = -P1[0];
            T[1, 2] = -P1[1];
        }

        /// <summary>
        /// 原点及角度建立坐标系
        /// </summary>
        /// <param name="Po">原点坐标</param>
        /// <param name="Theta">角度</param>
        public CoordinateTransformer(PointF Po, double theta)
        {
            double radians = theta * Math.PI / 180; // 将角度值转换为弧度值
            //1.新坐标系的原点：我们可以将 P1 作为新坐标系的原点。因此，新坐标系的原点坐标为 (x1, y1)。
            P1 = Vector<double>.Build.Dense(new double[] { Po.X, Po.Y });

            //2.计算坐标系的基向量：
            E1 = Vector<double>.Build.Dense(new double[] { Math.Cos(radians), Math.Sin(radians) }); // 新x轴向量
            E2 = Vector<double>.Build.Dense(new double[] { -Math.Sin(radians), Math.Cos(radians) }); // 新y轴向量

            //3.计算旋转矩阵：我们可以使用基向量组成的矩阵作为旋转矩阵。具体地，我们可以将基向量 e1 和 e2 分别作为新坐标系下的 $x$ 和 $y$ 轴，然后组成一个矩阵，再对这个矩阵求逆即可得到旋转矩阵。代码实现：
            // Compute rotation matrix
            R = Matrix<double>.Build.DenseOfColumnVectors(E1, E2);
            InvR = R.Inverse();

            //4.平移矩阵：由于我们选择的原点是 P1，因此我们需要对所有点进行平移，使得新坐标系的原点与笛卡尔坐标系的原点重合。具体地，我们可以使用以下平移矩阵：
            // Compute translation matrix
            T = Matrix<double>.Build.DenseIdentity(3);
            T[0, 2] = -P1[0];
            T[1, 2] = -P1[1];
        }

        /// <summary>
        /// Word坐标系下的点转换成Local坐标系下的点
        /// </summary>
        /// <param name="x">Word坐标系下的点X坐标</param>
        /// <param name="y">Word坐标系下的点Y坐标</param>
        /// <returns>返回Local坐标系下的点</returns>
        public Vector<double> ToNewCoord(double x, double y)
        {
            Vector<double> pOld = Vector<double>.Build.Dense(new double[] { x, y });
            Vector<double> pNew = InvR * (pOld - P1);
            return pNew;
        }

        /// <summary>
        /// Local坐标系下的点转换成Word坐标系下的点
        /// </summary>
        /// <param name="x">Local坐标系下的点X坐标</param>
        /// <param name="y">Local坐标系下的点Y坐标</param>
        /// <returns>返回Word坐标系下的点</returns>
        public Vector<double> ToOldCoord(double x, double y)
        {
            Vector<double> pNew = Vector<double>.Build.Dense(new double[] { x, y });
            Vector<double> pOld = R * pNew + P1;
            return pOld;
        }
    }
}
