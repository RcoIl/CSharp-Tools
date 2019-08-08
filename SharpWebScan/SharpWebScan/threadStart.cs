namespace SharpWebScan
{
    public class threadStart
    {
        private string ipss = "";
        private string port = "";

        public threadStart(string ip,string port)
        {
            this.ipss = ip;
            this.port = port;
        }

        public void method_0()
        {
            Program.GetAll(this.ipss, this.port);
        }
    }
}