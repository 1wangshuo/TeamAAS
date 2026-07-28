using Newtonsoft.Json;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamAAS.Robot.Enums;

namespace TeamAAS.Robot.Models
{
    /// <summary>
    /// 机器人点位
    /// </summary>
    public class RPoint : BindableBase
    {
        private int _Number;

        /// <summary>
        /// 点编号
        /// </summary>
        public int Number
        {
            get { return _Number; }
            set { SetProperty(ref _Number, value); }
        }

        private string _Label;
        /// <summary>
        /// 点位标签
        /// </summary>
        public string Label
        {
            get { return _Label; }
            set { SetProperty(ref _Label, value); }
        }



        private float _X;
        public float X
        {
            get { return _X; }
            set { SetProperty(ref _X, value); }
        }
        private float _Y;
        public float Y
        {
            get { return _Y; }
            set { SetProperty(ref _Y, value); }
        }
        private float _Z;
        public float Z
        {
            get { return _Z; }
            set { SetProperty(ref _Z, value); }
        }
        private float _U;
        public float U
        {
            get { return _U; }
            set { SetProperty(ref _U, value); }
        }
        private float _V;
        public float V
        {
            get { return _V; }
            set { SetProperty(ref _V, value); }
        }
        private float _W;
        public float W
        {
            get { return _W; }
            set { SetProperty(ref _W, value); }
        }

        private RobotHand _Hand;
        public RobotHand Hand
        {
            get { return _Hand; }
            set { SetProperty(ref _Hand, value); }
        }
        private int _Local;
        public int Local
        {
            get { return _Local; }
            set { SetProperty(ref _Local, value); }
        }
        private int _Tool;
        public int Tool
        {
            get { return _Tool; }
            set { SetProperty(ref _Tool, value); }
        }

        private string _Description;

        /// <summary>
        /// 描述
        /// </summary>
        public string Description
        {
            get { return _Description; }
            set { SetProperty(ref _Description, value); }
        }

        public RPoint()
        {
            Number = 0;
            Label = "";
            X = 0;
            Y = 0;
            Z = 0;
            U = 0;
            V = 0;
            W = 0;
            Local = 0;
            Tool = 0;
            Hand = RobotHand.Right;
            Description = "";
        }

        public RPoint(int number,string label, float x, float y, float z, float u, float v, float w, int local, int tool, RobotHand hand, string description = "")
        {
            Number = number;
            Label = label;
            X = x;
            Y = y;
            Z = z;
            U = u;
            V = v;
            W = w;
            Local = local;
            Tool = tool;
            Hand = hand;
            Description = description;
        }

        public RPoint(int number)
        {
            Number = number;
            Label = "";
            X = 0;
            Y = 0;
            Z = 0;
            U = 0;
            V = 0;
            W = 0;
            Local = 0;
            Tool = 0;
            Hand = RobotHand.Right;
            Description = "";
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is RPoint && ((RPoint)obj).Number == this.Number && ((RPoint)obj).Label == this.Label && ((RPoint)obj).Description == this.Description && ((RPoint)obj).X == this.X && ((RPoint)obj).Y == this.Y && ((RPoint)obj).Z == this.Z && ((RPoint)obj).U == this.U && ((RPoint)obj).V == this.V && ((RPoint)obj).W == this.W && ((RPoint)obj).Local == this.Local)
            {
                return true;
            }
            else { return false; }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Number},{Label},{X:F3},{Y:F3},{Z:F3},{U:F3},{V:F3},{W:F3},{Local},{Tool},{Hand}";
        }

        public RPoint Clone()
        {

            return new RPoint(this.Number, this.Label, this.X, this.Y, this.Z, this.U, this.V, this.W, this.Local, this.Tool, this.Hand, this.Description);
        }
    }
}
