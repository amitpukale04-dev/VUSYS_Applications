using VUSYSTimeProWebApp.Models;

namespace VUSYSTimeProWebApp.Contracts
{
    public interface IState
    {
        /// <summary>
        /// This Method is Used to Get State List.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        Task<List<StateM>> GetStateList(int ID, string Type);

        /// <summary>
        /// This Method is Used to Get State Details.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<StateM> GetStateDetails(int? id);

        /// <summary>
        /// This Method is Used to Save Update State Details.
        /// </summary>
        /// <param name="_objStatesM"></param>
        /// <returns></returns>
        Task<string> SaveUpdateStateDetails(StateM _objStatesM);
    }
}
