using System;
using System.Collections.ObjectModel;
using Prism.Mvvm;

namespace TeamAAS.Robot.Models
{
    /// <summary>
    /// 托盘模型（简化版，从旧项目移植）
    /// </summary>
    public class Pallet : BindableBase
    {
        private int _id;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private double _originX;
        public double OriginX
        {
            get => _originX;
            set => SetProperty(ref _originX, value);
        }

        private double _originY;
        public double OriginY
        {
            get => _originY;
            set => SetProperty(ref _originY, value);
        }

        private double _originZ;
        public double OriginZ
        {
            get => _originZ;
            set => SetProperty(ref _originZ, value);
        }

        private int _rows = 1;
        public int Rows
        {
            get => _rows;
            set => SetProperty(ref _rows, value);
        }

        private int _cols = 1;
        public int Cols
        {
            get => _cols;
            set => SetProperty(ref _cols, value);
        }

        private double _rowSpacing;
        public double RowSpacing
        {
            get => _rowSpacing;
            set => SetProperty(ref _rowSpacing, value);
        }

        private double _colSpacing;
        public double ColSpacing
        {
            get => _colSpacing;
            set => SetProperty(ref _colSpacing, value);
        }
    }
}
