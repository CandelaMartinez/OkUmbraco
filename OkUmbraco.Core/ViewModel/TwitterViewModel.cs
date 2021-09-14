using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OkUmbraco.Core.ViewModel
{
    //aqui guardo la data de twitter para renderizar los ultimos tweets
    public class TwitterViewModel
    {
        public string TwitterHandle { get; set; }

        public bool Error { get; set; }

        //twitter API devuelve la data en Json
        public string Json { get; set; }

        public string Message { get; set; }



    }
}
