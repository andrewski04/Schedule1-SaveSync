using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schedule1_SaveSync.SaveSync.Interfaces
{
    internal interface ISaveManager
    { // this is incredibly non final
        public List<ISaveable> SaveGames { get; set; }
        public bool LoadSave(ISaveable save);
        public bool SaveGame(ISaveable save);
    }
}
