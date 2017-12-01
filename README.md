# Poor-Man-s-Prallelism
This project consist of two applications, a server and a client. Together renders part of the Mandelbrot set, due to their iterative nature the rendering is time-consuming, but the work is quite easy to parallize.. The server accept requests on a TCP port, the client spread the workload over a set of servers.
