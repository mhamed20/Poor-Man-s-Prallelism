using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class SynchronousSocketClient
{
    public class Connect
    {
        public void getWork(double mnC_re, double mnC_im, double mxC_re, double mxC_im, int xlc, int ylc, int mx_n, string hst, int prt, int ii, int jj)
        {
            minC_re = mnC_re;
            minC_im = mnC_im;
            maxc_re = mxC_re;
            maxC_im = mxC_im;
            xloc = xlc;
            yloc = ylc;
            max_n = mx_n;
            host = hst;
            port = prt;
            i = ii;
            j = jj;
            status = false;
        }

        public bool getStatus() { return status; }

        public byte[] getpart() { return part; }


        private
         double minC_re;
        double minC_im;
        double maxc_re;
        double maxC_im;
        int xloc;
        int yloc;
        int max_n;
        string host;
        int port;
        int i;
        int j;

        byte[] part;

        bool status;


        public void SendWork()
        {
            // Data buffer for incoming data.  
            byte[] bytes = new byte[xloc * yloc];

            // Connect to a remote device.  
            try
            {
                // Establish the remote endpoint for the socket.                 
                IPAddress ipAddress = IPAddress.Parse(host);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create a TCP/IP  socket.  
                Socket sender = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.  
                try
                {
                    // Connect to server
                    sender.Connect(remoteEP);
                    Console.WriteLine("Connected to {0}",
                    sender.RemoteEndPoint.ToString());

                    // determine string to send

                    string work = "";
                    double[] w = new double[9];

                    w[0] = minC_re;
                    w[1] = minC_im;
                    w[2] = maxc_re;
                    w[3] = maxC_im;
                    w[4] = xloc;
                    w[5] = yloc;
                    w[6] = (double)max_n;
                    w[7] = i;
                    w[8] = j;

                    for (int m = 0; m < 9; m++)
                    {
                        work = string.Concat(work, w[m].ToString());
                        work = string.Concat(work, " ");
                    }

                    work = string.Concat(work, "<EOF>");

                    // Encode the data string into a byte array.  
                    byte[] msg = Encoding.ASCII.GetBytes(work);

                    // Send the data through the socket.  
                    int bytesSent = sender.Send(msg);

                    // Receive the response from the remote device.  
                    int bytesRec = sender.Receive(bytes);


                    //Release the socket.
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                    status = true;
                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            part = bytes;
        }
    }



    public static int Main(String[] args)
    {
        string spec;

        Console.WriteLine("Enter the specifications :");
        spec = Console.ReadLine();
        
        double minC_im;
        double minC_re;
        double maxC_re;
        double maxC_im;
        int max_n;
        int x;
        int y;
        int divs;

        bool accept;

        string[] data_str = spec.Split(' ');

        // extract information
        accept = double.TryParse(data_str[0], out minC_re);
        accept = double.TryParse(data_str[1], out minC_im);
        accept = double.TryParse(data_str[2], out maxC_re);
        accept = double.TryParse(data_str[3], out maxC_im);
        accept = int.TryParse(data_str[4], out max_n);
        accept = int.TryParse(data_str[5], out x);
        accept = int.TryParse(data_str[6], out y);
        accept = int.TryParse(data_str[7], out divs);

        // determining hosts and ports
        ;
        string[] host = new string[divs];
        int[] port = new int[divs];

        for (int i = 0; i < divs; i++)
        {
            string server = data_str[8 + i];
            string[] server_spec = server.Split(':');

            host[i] = server_spec[0];
            port[i] = Int32.Parse(server_spec[1]);
        }

        // devide and spread the work

        int xloc, yloc;

        // determine the best devision of total servers

        List<int> primenumbers = new List<int> { 1, 2, 3, 5, 7, 9, 11, 13 };

        // check if divs is a prime number

        int a, b;
        bool check = primenumbers.IndexOf(divs) != -1;

        if (!check)
        {
            int index = 1;
            while (divs % primenumbers[index] > 0) { index++; }

            a = divs / primenumbers[index];
            b = divs / a;
        }

        else
        {
            a = divs;
            b = 1;
        }

        // local part
        xloc = x / a;
        yloc = y / (divs / b);

        byte[] Entire_Pic = new Byte[x * y];

        byte[] part = new Byte[xloc * yloc];


        // Prepare connections

        Connect [] servers = new Connect[divs];
        Thread [] connection_thread = new Thread[divs];

        for (int i = 0; i < a; i++)
            for (int j = 0; j < b; j++)
                servers[i + j * b] = new Connect();

        for (int i = 0; i < a; i++)
        {
            for (int j = 0; j < b; j++)
            {
                int server_ind = i + b * j;

                // send the work load and store received result
                
                servers[i + j * b].getWork(minC_re, minC_im, maxC_re, maxC_im, xloc, yloc, max_n, host[server_ind], port[server_ind], i, j);

                connection_thread[i + j * b] = new Thread(new ThreadStart(servers[i + j * b].SendWork));

                connection_thread[i + j * b].Start();
            }
        }

        
        // Check if all servers responded
        bool Ok = false;

        while (!Ok)
        {
            for (int i = 0; i < a; i++)
            {
                for (int j = 0; j < b; j++)
                {
                    if (servers[i + j * b].getStatus() == true)
                    {                       
                        Console.WriteLine("All servers responded.");

                        // copy the parts to the global picture
                        for (int m = 0; m < a; m++)
                        {
                            for (int n = 0; n < b; n++)
                            {

                                for (int t = 0; t < xloc * yloc; t++)
                                    Entire_Pic[t + i * xloc + j * yloc] = servers[i + j * b].getpart()[t];
                            }
                        }

                        Ok = true;

                    }
                }
            }
        }

            // convert to integers
            int[,] Pic = new int[x, y];

            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < y; j++)
                {
                    Pic[i, j] = Entire_Pic[i + j * y];
                }
            }


            // save as PGM image

            StreamWriter sw = new StreamWriter("result.pgm");

            // write header
            sw.WriteLine("P2");
            sw.WriteLine(x + " " + y);
            sw.WriteLine(255);

            // write data
            for (int i = 0; i < x; i++)
                for (int j = 0; j < y; j++)
                    sw.Write(Pic[i, j] + " ");
            sw.Close();


            return 0;
        }
    }
