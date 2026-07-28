using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamAAS.Robot.Core
{
    /// <summary>
    /// 工具坐标系
    /// </summary>
    public class ToolCoord
    {
        /// <summary>
        /// 工具坐标编号
        /// </summary>
        public int ToolNumber { get; set; } = 0;
        /// <summary>
        /// 工具坐标X
        /// </summary>
        public double X { get; set; } = 0;
        /// <summary>
        /// 工具坐标Y
        /// </summary>
        public double Y { get; set; } = 0;
        /// <summary>
        /// 工具坐标
        /// </summary>
        public Vector<double> Coord { get; set; } = Vector<double>.Build.Dense(new double[] { 0, 0 });

        /// <summary>
        /// 创建工具坐标
        /// </summary>
        /// <param name="x">x坐标</param>
        /// <param name="y">Y坐标</param>
        public void SetTool(double x, double y)
        {
            SetTool(Vector<double>.Build.Dense(new double[] { x, y }));
        }

        /// <summary>
        /// 创建工具坐标
        /// </summary>
        /// <param name="coord">向量坐标</param>
        public void SetTool(Vector<double> coord)
        {
            X = coord[0]; Y = coord[1]; Coord = coord;
        }

        /// <summary>
        /// 已知Tool 0下的坐标和tool n下的坐标，计算Tool n
        /// </summary>
        /// <param name="x1">tool 0下的坐标，J4关节的旋转中心点</param>
        /// <param name="y1">tool 0下的坐标，J4关节的旋转中心点</param>
        /// <param name="u1">tool 0下的坐标，J4关节的旋转中心点</param>
        /// <param name="x2">tool n下的坐标</param>
        /// <param name="y2">tool n下的坐标</param>
        public void ComputeTool(double x1, double y1, double u1, double x2, double y2)
        {
            Vector<double> P0 = Vector<double>.Build.Dense(new double[] { x1, y1 });
            Vector<double> P1 = Vector<double>.Build.Dense(new double[] { x2, y2 });
            ComputeTool(P0, P1, u1);
        }

        /// <summary>
        /// 两点法计算工具坐标（已知Tool 0下的两个点的坐标计算 Tool n）
        /// </summary>
        /// <param name="x1">Tool 0下点1坐标</param>
        /// <param name="y1">Tool 0下点1坐标</param>
        /// <param name="u1">Tool 0下点1坐标</param>
        /// <param name="x2">Tool 0下点2坐标</param>
        /// <param name="y2">Tool 0下点2坐标</param>
        /// <param name="u2">Tool 0下点2坐标</param>
        public void ComputeTool(double x1, double y1, double u1, double x2, double y2, double u2)
        {
            Vector<double> P1 = Vector<double>.Build.Dense(new double[] { x1, y1 });
            Vector<double> P2 = Vector<double>.Build.Dense(new double[] { x2, y2 });
            ComputeTool(P1, P2, u1, u2);
        }

        /// <summary>
        /// 已知Tool 0下的坐标和tool n下的坐标，计算Tool n
        /// </summary>
        /// <param name="tool0">tool 0下的坐标，J4关节的旋转中心点</param>
        /// <param name="tooln">tool n下的坐标，</param>
        /// <param name="Angle">Tool 0旋转角度，J4关节的旋转角度</param>
        public void ComputeTool(Vector<double> P0, Vector<double> P1, double Angle)
        {
            //Pb=𝑹^(−𝟏)*(𝑷1-𝑷0)

            double rad = AngleToRad(Angle);

            // 创建旋转矩阵
            Matrix<double> rotationMatrix = Matrix<double>.Build.DenseOfArray(new double[,]
            {
                { Math.Cos(rad), -Math.Sin(rad) },
                { Math.Sin(rad), Math.Cos(rad) }
            });

            //逆矩阵
            var InvR = rotationMatrix.Inverse();

            Vector<double> Pb = InvR * (P1 - P0);
            Coord = Pb;
            X = Pb[0];
            Y = Pb[1];
        }

        /// <summary>
        /// 两点法计算工具坐标（已知Tool 0下的两个点的坐标计算 Tool n）
        /// </summary>
        /// <param name="P1">点1坐标</param>
        /// <param name="P2">点2坐标</param>
        /// <param name="Angle1">点1角度</param>
        /// <param name="Angle2">点2角度</param>
        public void ComputeTool(Vector<double> P1, Vector<double> P2, double Angle1, double Angle2)
        {
            //R1* 𝑷𝟑+ 𝑷𝟏= R2* 𝑷𝟑+ 𝑷𝟐
            //𝑷𝟑= (R1 −R𝟐)^−𝟏*(𝑷𝟐−𝑷𝟏)
            Angle1 = AngleToRad(Angle1);
            Angle2 = AngleToRad(Angle2);

            // 创建旋转矩阵
            Matrix<double> R1 = Matrix<double>.Build.DenseOfArray(new double[,]
            {
                { Math.Cos(Angle1), -Math.Sin(Angle1) },
                { Math.Sin(Angle1), Math.Cos(Angle1) }
            });

            Matrix<double> R2 = Matrix<double>.Build.DenseOfArray(new double[,]
            {
                { Math.Cos(Angle2), -Math.Sin(Angle2) },
                { Math.Sin(Angle2), Math.Cos(Angle2) }
            });

            Matrix<double> RN = R1 - R2;
            Vector<double> PN = P2 - P1;

            //Vector<double> P3 = RN.Inverse() * PN;
            Vector<double> P3 = RN.Solve(PN);
            Coord = P3;
            X = P3[0];
            Y = P3[1];
        }

        /// <summary>
        /// 已知Tool n下的坐标和Tool n旋转的角度，计算Tool 0下的坐标
        /// </summary>
        /// <param name="tooln">Tool n下的坐标</param>
        /// <param name="Angle">tool n旋转的角度</param>
        /// <returns></returns>
        public Vector<double> GetTool0Coord(Vector<double> tooln, double Angle)
        {
            Coord = Vector<double>.Build.Dense(new double[] { X, Y });
            double rad = AngleToRad(Angle);
            // 创建旋转矩阵
            Matrix<double> rotationMatrix = Matrix<double>.Build.DenseOfArray(new double[,]
            {
                { Math.Cos(rad), -Math.Sin(rad) },
                { Math.Sin(rad), Math.Cos(rad) }
            });

            Vector<double> P0 = rotationMatrix * (-Coord) + tooln;
            return P0;
        }

        /// <summary>
        /// 已知Tool 0下的坐标和Tool 0旋转的角度，计算Tool n下的坐标
        /// </summary>
        /// <param name="tool0">Tool 0下的坐标</param>
        /// <param name="Angle">tool 0旋转的角度</param>
        /// <returns></returns>
        public Vector<double> GetToolnCoord(Vector<double> tool0, double Angle)
        {
            Coord = Vector<double>.Build.Dense(new double[] { X, Y });
            double rad = AngleToRad(Angle);
            // 创建旋转矩阵
            Matrix<double> rotationMatrix = Matrix<double>.Build.DenseOfArray(new double[,]
            {
                { Math.Cos(rad), -Math.Sin(rad) },
                { Math.Sin(rad), Math.Cos(rad) }
            });

            Vector<double> P0 = rotationMatrix * Coord + tool0;
            return P0;
        }

        /// <summary>
        /// 角度转换成弧度
        /// </summary>
        /// <param name="angle">角度</param>
        /// <returns></returns>
        public static double AngleToRad(double angle)
        {
            return Math.PI * angle / 180;
        }

        /// <summary>
        /// 弧度转换成度数
        /// </summary>
        /// <param name="radian">弧度</param>
        /// <returns></returns>
        public static double RadToAngle(double radian)
        {
            return radian * 180 / Math.PI; ;
        }
    }
}
