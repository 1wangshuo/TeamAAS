using Newtonsoft.Json;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TeamAAS.Robot.Interfaces;

namespace TeamAAS.Robot.Models.Robot
{
    /// <summary>
    /// 多机器人的点位信息
    /// </summary>
    public class RobotPoint : BindableBase
    {

        private ObservableCollection<RobotPointStore> _Robots;

        /// <summary>
        /// 机器人集合
        /// </summary>
        public ObservableCollection<RobotPointStore> Robots
        {
            get => _Robots;
            set => SetProperty(ref _Robots, value);
        }

        /// <summary>
        /// 增加机器人
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        public void AddRobot(Guid id,string name)
        {
            if (Robots == null)
            {
                Robots = new ObservableCollection<RobotPointStore>();
            }
            if (!Robots.Any(r => r.Id == id))
            {
                Robots.Add(new RobotPointStore(id, name));
            }
            else
            {
                throw new Exception("机器人已存在");
            }
        }

        /// <summary>
        /// 移除机器人
        /// </summary>
        /// <param name="id"></param>
        /// <exception cref="Exception"></exception>
        public void RemoveRobot(Guid id)
        {
            var robot = Robots.FirstOrDefault(r => r.Id == id);
            if (robot != null)
            {
                Robots.Remove(robot);
            }
            else
            {
                throw new Exception("机器人不存在");
            }
        }

        /// <summary>
        /// 修改机器人名称
        /// </summary>
        /// <param name="id"></param>
        /// <param name="newName"></param>
        /// <exception cref="Exception"></exception>
        public void UpdateRobotName(Guid id, string newName)
        {
            var robot = Robots.FirstOrDefault(r => r.Id == id);
            if (robot != null)
            {
                robot.Name = newName;
            }
            else
            {
                throw new Exception("机器人不存在");
            }
        }

        public RobotPoint()
        {
            Robots = new ObservableCollection<RobotPointStore>();
        }

        public RobotPoint(RobotInfo[] robotInfos)
        {
            Robots = new ObservableCollection<RobotPointStore>();
            foreach (var info in robotInfos)
            {
                Robots.Add(new RobotPointStore(info.Id, info.RobotName));
            }
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <returns>新的 RobotPoint 实例</returns>
        public RobotPoint Clone()
        {
            return JsonConvert.DeserializeObject<RobotPoint>(JsonConvert.SerializeObject(this));
        }
    }

    /// <summary>
    /// 机器人点位的容器
    /// </summary>
    public class RobotPointStore : BindableBase
    {
        private Guid _id;
        /// <summary>
        /// 机器人唯一 Id（可用 GUID 或自定义字符串）
        /// </summary>
        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _name;

        /// <summary>
        /// 机器人名称
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private ObservableCollection<RPoint> _Points;
        /// <summary>
        /// 点位集合
        /// </summary>
        public ObservableCollection<RPoint> Points
        {
            get { return _Points; }
            set { SetProperty(ref _Points, value); }
        }

        private ObservableCollection<Pallet> _pallets;

        /// <summary>
        /// 托盘集合
        /// </summary>
        public ObservableCollection<Pallet> Pallets
        {
            get => _pallets;
            set => SetProperty(ref _pallets, value);
        }

        public RobotPointStore()
        {
            Points = new ObservableCollection<RPoint>();
            Pallets=new ObservableCollection<Pallet>();
        }

        public RobotPointStore(Guid id, string name)
        {
            Id = id;
            Name = name;
            Points = new ObservableCollection<RPoint>();
            Pallets = new ObservableCollection<Pallet>();
            for (int i = 0; i < 1000; i++)
            {
                Points.Add(new RPoint(i));
            }
        }
    }
}
