using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Car_Parking.Scripts.GameInput
{
    [System.Serializable]
    public class InputMapGroup
    {
        [SerializeField] private InputGroupsEnum groupName;
        public InputGroupsEnum GroupName => groupName;

        [Tooltip("all possible maps in the group")]
        [SerializeField] private List<InputMapEnum> maps;

        public bool Contains(InputMapEnum mapToActivate)
        {
            return maps.Contains(mapToActivate);
        }
        public IEnumerable<InputMapEnum> GetAllMaps()
        {
            return maps.AsReadOnly();
        }
    }
}