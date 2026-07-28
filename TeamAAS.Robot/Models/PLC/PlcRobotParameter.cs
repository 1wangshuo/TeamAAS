using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamAAS.Robot.Models.PLC
{
    public class PlcRobotParameter : BindableBase
    {
        private CommandParameter _CommandParameter=new CommandParameter();
        public CommandParameter CommandParameter
        {
            get { return _CommandParameter; }
            set { SetProperty(ref _CommandParameter, value); }
        }

        private StateParameter _StateParameter=new StateParameter();
        public StateParameter StateParameter
        {
            get { return _StateParameter; }
            set { SetProperty(ref _StateParameter, value); }
        }
    }

    public class CommandParameter : BindableBase
    {
        private string _X;
        public string X
        {
            get { return _X; }
            set { SetProperty(ref _X, value); }
        }

        private string _Y;
        public string Y
        {
            get { return _Y; }
            set { SetProperty(ref _Y, value); }
        }

        private string _Z;
        public string Z
        {
            get { return _Z; }
            set { SetProperty(ref _Z, value); }
        }

        private string _U;
        public string U
        {
            get { return _U; }
            set { SetProperty(ref _U, value); }
        }

        private string _Speed;
        public string Speed
        {
            get { return _Speed; }
            set { SetProperty(ref _Speed, value); }
        }

        private string _Accel;
        public string Accel
        {
            get { return _Accel; }
            set { SetProperty(ref _Accel, value); }
        }

        private string _WaitVacuumOn;
        public string WaitVacuumOn
        {
            get { return _WaitVacuumOn; }
            set { SetProperty(ref _WaitVacuumOn, value); }
        }

        private string _WaitVacuumOff;
        public string WaitVacuumOff
        {
            get { return _WaitVacuumOff; }
            set { SetProperty(ref _WaitVacuumOff, value); }
        }

        private string _Limz;
        public string Limz
        {
            get { return _Limz; }
            set { SetProperty(ref _Limz, value); }
        }

        private string _ExecuteMove;
        public string ExecuteMove
        {
            get { return _ExecuteMove; }
            set { SetProperty(ref _ExecuteMove, value); }
        }

        private string _VacuumOn;
        public string VacuumOn
        {
            get { return _VacuumOn; }
            set { SetProperty(ref _VacuumOn, value); }
        }

        private string _VacuumOff;
        public string VacuumOff
        {
            get { return _VacuumOff; }
            set { SetProperty(ref _VacuumOff, value); }
        }

        private string _Distance;
        public string Distance
        {
            get { return _Distance; }
            set { SetProperty(ref _Distance, value); }
        }

        private string _JogCmd;
        public string JogCmd
        {
            get { return _JogCmd; }
            set { SetProperty(ref _JogCmd, value); }
        }
    }

    public class StateParameter : BindableBase
    {
        private string _X;
        public string X
        {
            get { return _X; }
            set { SetProperty(ref _X, value); }
        }

        private string _Y;
        public string Y
        {
            get { return _Y; }
            set { SetProperty(ref _Y, value); }
        }

        private string _Z;
        public string Z
        {
            get { return _Z; }
            set { SetProperty(ref _Z, value); }
        }

        private string _U;
        public string U
        {
            get { return _U; }
            set { SetProperty(ref _U, value); }
        }

        private string _MoveFinish;
        public string MoveFinish
        {
            get { return _MoveFinish; }
            set { SetProperty(ref _MoveFinish, value); }
        }

        private string _Error;
        public string Error
        {
            get { return _Error; }
            set { SetProperty(ref _Error, value); }
        }

        private string _VacuumOn;
        public string VacuumOn
        {
            get { return _VacuumOn; }
            set { SetProperty(ref _VacuumOn, value); }
        }

        private string _VacuumOff;
        public string VacuumOff
        {
            get { return _VacuumOff; }
            set { SetProperty(ref _VacuumOff, value); }
        }
    }
}
