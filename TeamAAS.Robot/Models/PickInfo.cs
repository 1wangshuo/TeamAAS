using Newtonsoft.Json;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamAAS.Robot.Enums;

namespace TeamAAS.Robot.Models
{
    /// <summary>
    /// 取料方式
    /// </summary>
    public class PickInfo : BindableBase
    {
		private PickPlaceModel _PickPlaceModel;

		/// <summary>
		/// 取料方式
		/// </summary>
		public PickPlaceModel PickPlaceModel
        {
			get { return _PickPlaceModel; }
			set { SetProperty(ref _PickPlaceModel,value); }
		}

		private int _Inhal = -1;

		/// <summary>
		/// 吸气/夹紧
		/// </summary>
        public int Inhal
        {
			get { return _Inhal; }
			set { SetProperty(ref _Inhal, value); }
		}

        private int _Blow = -1;

        /// <summary>
        /// 吹气/松开
        /// </summary>
        public int Blow
        {
            get { return _Blow; }
            set { SetProperty(ref _Blow, value); }
        }

        public PickInfo Clone()
        {
            return new PickInfo()
            {
                PickPlaceModel = this.PickPlaceModel,
                Inhal = this.Inhal,
                Blow = this.Blow
            };
        }
    }
}
