using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

public class SynchronousSocketListener
{
    // Incoming data from the client.  
    public static string data = null;

    public class Connect
    {
        public void serverSpec(string ho, int po) { host = ho; port = po; }

        private
        string host;
        int port;

        public void Listen()
        {
            // Data buffer for incoming data.  
            byte[] bytes = new Byte[1024];

            // Establish the local endpoint for the socket.  
          
            IPAddress ipAddress = IPAddress.Parse(host);
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

            // Create a TCP/IP socket.  
            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and   
            // listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                // Start listening for connections.  
                while (true)
                {
                    Console.WriteLine("Waiting for a connection...");
                    // Program is suspended while waiting for an incoming connection.  
                    Socket handler = listener.Accept();
                    data = null;

                    // An incoming connection needs to be processed.  
                    while (true)
                    {
                        bytes = new byte[1024];
                        int bytesRec = handler.Receive(bytes);

                        data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        if (data.IndexOf("<EOF>") > -1)
                        {
                            break;
                        }
                    }
                    String data_new = data.Substring(0, data.Length - 5);

                    // get values
                    double minC_re;
                    double minC_im;
                    double maxC_re;
                    double maxC_im;
                    int xloc;
                    int yloc;
                    int max_n;
                    int i;
                    int j;
                                 
                    bool accept;
                   
                    string[] data_str = data_new.Split(' ');

                    // extract information
                    accept = double.TryParse(data_str[0], out minC_re);
                    accept = double.TryParse(data_str[1], out minC_im);
                    accept = double.TryParse(data_str[2], out maxC_re);
                    accept = double.TryParse(data_str[3], out maxC_im);
                    accept = int.TryParse(data_str[4], out xloc);
                    accept = int.TryParse(data_str[5], out yloc);
                    accept = int.TryParse(data_str[6], out max_n);
                    accept = int.TryParse(data_str[7], out i);
                    accept = int.TryParse(data_str[8], out j);

                    byte[] part_pic = new Byte[xloc * yloc];

                    // Calculation                   

                    for (int x = i * xloc; x < i * xloc + xloc; x++)
                        for (int y = j * yloc; y < j * yloc + yloc; y++)
                        {
                            for (double cx = minC_re; cx < maxC_re; cx++)

                                for (double cy = minC_im; cy < maxC_im; cy++)
                                {
                                    // initial values
                                    double tx = x + cx;
                                    double ty = y + cy;

                                    // iterate 

                                    int n = 0;

                                    do
                                    {
                                        double znewx = tx * tx - ty * ty + tx;
                                        double znewy = ty + 2 * tx * ty;

                                        double absZ = Math.Sqrt(znewx * znewx - znewy*znewy);
                                        if (absZ > Math.Sqrt(cx * cx - cy * cy))
                                        {
                                            byte pxl = (byte)(n % 256);

                                            part_pic[(x - i * xloc) + y * (yloc - j * yloc)] = pxl;
                                            break;
                                        }
                                        else n++;

                                        tx = znewx;
                                        ty = znewy;

                                    } while (n < max_n);

                                }
                        }

                    // send back results

                    handler.Send(part_pic);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }
}


public static int Main(String[] args)
    {
        // prepare servers for listening

        Connect [] connections = new Connect[4];
        Thread [] connection_thread = new Thread[4];

        for (int i = 0; i < 4; i++)          
            connections[i] = new Connect();

        for (int i = 0; i < 4; i++)
        {               
            connections[i].serverSpec("127.0.0.1", 8080+i);
            connection_thread[i] = new Thread(new ThreadStart(connections[i].Listen));
            connection_thread[i].Start();            
        }

        return 0;
    }
}