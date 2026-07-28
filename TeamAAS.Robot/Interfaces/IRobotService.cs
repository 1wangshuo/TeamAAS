using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamAAS.Robot.Models;

namespace TeamAAS.Robot.Interfaces
{
    /// <summary>
    /// 提供机器人生命周期管理的接口，包括创建、获取及注销机器人实例。
    /// </summary>
    public interface IRobotService
    {
        /// <summary>
        /// 异步创建并注册一个机器人实例。
        /// </summary>
        /// <param name="id">要创建机器人的唯一标识（GUID）。</param>
        /// <param name="robotInfo">用于初始化机器人的信息对象，不能为空。</param>
        /// <returns>
        /// 返回一个 <see cref="Task{Boolean}"/>；任务完成时布尔值指示创建是否成功。
        /// </returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="robotInfo"/> 为 null 时抛出。</exception>
        Task<bool> CreateRobotAsync(Guid id, RobotInfo robotInfo);

        /// <summary>
        /// 同步创建并注册一个机器人实例。
        /// </summary>
        /// <param name="id">要创建机器人的唯一标识（GUID）。</param>
        /// <param name="robotInfo">用于初始化机器人的信息对象，不能为空。</param>
        /// <returns>返回布尔值，指示创建是否成功。</returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="robotInfo"/> 为 null 时抛出。</exception>
        bool CreateRobot(Guid id, RobotInfo robotInfo);

        /// <summary>
        /// 获取已注册的机器人实例（同步）。通过 GUID 查找。
        /// </summary>
        /// <param name="id">要获取的机器人唯一标识（GUID）。</param>
        /// <returns>
        /// 已注册的 <see cref="IRobot"/> 实例；如果未找到对应机器人，返回 null（或实现可选择抛出异常）。
        /// </returns>
        IRobot GetRobot(Guid id);

        /// <summary>
        /// 异步获取已注册的机器人实例。通过 GUID 查找。
        /// </summary>
        /// <param name="id">要获取的机器人唯一标识（GUID）。</param>
        /// <returns>
        /// 一个任务，完成时返回对应的 <see cref="IRobot"/> 实例；若未找到可返回 null。
        /// </returns>
        Task<IRobot> GetRobotAsync(Guid id);

        /// <summary>
        /// 获取所有已注册的机器人实例（同步）。
        /// </summary>
        /// <returns></returns>
        IReadOnlyCollection<IRobot> GetAllRobots();

        /// <summary>
        /// 获取所有已注册的机器人实例（异步）。
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyCollection<IRobot>> GetAllRobotsAsync();

        /// <summary>
        /// 尝试获取已注册的机器人实例（同步）。通过 GUID 查找。
        /// </summary>
        /// <param name="id"></param>
        /// <param name="robot"></param>
        /// <returns></returns>
        bool TryGetRobot(Guid id, out IRobot robot);

        /// <summary>
        /// 尝试获取已注册的机器人实例（异步）。通过 GUID 查找。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<(bool found, IRobot robot)> TryGetRobotAsync(Guid id);

        /// <summary>
        /// 检查机器人是否已注册（同步）。通过 GUID 检查。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        bool ContainsRobot(Guid id);

        /// <summary>
        /// 检查机器人是否已注册（异步）。通过 GUID 检查。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<bool> ContainsRobotAsync(Guid id);

        /// <summary>
        /// 移除已注册的机器人实例（同步）。通过 GUID 移除。
        /// </summary>
        /// <param name="id"></param>
        /// <returns>返回是否成功移除</returns>
        bool RemoveRobot(Guid id);

        /// <summary>
        /// 移除已注册的机器人实例（异步）。通过 GUID 移除。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<bool> RemoveRobotAsync(Guid id);

        /// <summary>
        /// 异步注销并移除指定的机器人注册信息。通过 GUID 注销。
        /// </summary>
        /// <param name="id">要注销的机器人唯一标识（GUID）。</param>
        /// <returns>表示注销操作完成的任务。</returns>
        Task UnRegisterRobotAsync(Guid id);

        /// <summary>
        /// 同步注销并移除指定的机器人注册信息。通过 GUID 注销。
        /// </summary>
        /// <param name="id">要注销的机器人唯一标识（GUID）。</param>
        void UnRegisterRobot(Guid id);

        /// <summary>
        /// 获取已注册的机器人实例（同步）。通过机器人编号（int）查找。
        /// </summary>
        /// <param name="number">机器人编号（整型）。</param>
        /// <returns>
        /// 已注册的 <see cref="IRobot"/> 实例；如果未找到对应机器人，返回 null（或实现可选择抛出异常）。
        /// </returns>
        IRobot GetRobotByNumber(int number);

        /// <summary>
        /// 异步获取已注册的机器人实例。通过机器人编号（int）查找。
        /// </summary>
        /// <param name="number">机器人编号（整型）。</param>
        /// <returns>
        /// 一个任务，完成时返回对应的 <see cref="IRobot"/> 实例；若未找到可返回 null。
        /// </returns>
        Task<IRobot> GetRobotByNumberAsync(int number);

        /// <summary>
        /// 异步注销并移除指定的机器人注册信息。通过机器人编号（int）注销。
        /// </summary>
        /// <param name="number">要注销的机器人编号（整型）。</param>
        /// <returns>表示注销操作完成的任务。</returns>
        Task UnRegisterRobotByNumberAsync(int number);

        /// <summary>
        /// 同步注销并移除指定的机器人注册信息。通过机器人编号（int）注销。
        /// </summary>
        /// <param name="number">要注销的机器人编号（整型）。</param>
        void UnRegisterRobotByNumber(int number);

        /// <summary>
        /// 移除所有机器人
        /// </summary>
        void RemoveAllRobots();

        /// <summary>
        /// 初始化所有机器人
        /// </summary>
        /// <param name="robots"></param>
        /// <returns></returns>
        (bool IsSucceed, string Message) InitializeAllRobots(RobotInfo[] robots);

        /// <summary>
        /// 初始化所有机器人 （异步）
        /// </summary>
        /// <param name="robots"></param>
        Task<(bool IsSucceed, string Message)> InitializeAllRobotsAsync(RobotInfo[] robots);

        /// <summary>
        /// 更新机器人编号
        /// </summary>
        /// <param name="id"></param>
        /// <param name="newNumber"></param>
        void UpdateRobotNumber(Guid id, int newNumber);
    }
}
