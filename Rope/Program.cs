using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Rope
{
    class Program
    {
        //POINT struct for mouse position
        public struct POINT
        {
            public int X;
            public int Y;
        }
        //Import dll for mouse position
        [DllImport("user32.dll")]
        //Function that returns current mouse position to POINT variable
        public static extern bool GetCursorPos(out POINT lpPoint);

        //Declare gravity constant (not realistic value to try to add a realistic feel, 9.81 is too slow)
        //const float gravity = -9.81f;
        static void Main(string[] args)
        {
            var watch = new Stopwatch();
            //Initialize console
            Console.CursorVisible = false;

            //Point variable to store mouse position
            POINT mousePos = new POINT();

            //Setup calculations by prompting user to get window size
            Console.WriteLine("Welcome to Rope Simulator!\n");

            //Declare topLeft and botRight vectors to store the mouse position of the
            //top left and bottom right corners of the console window
            Vector2 topLeft = new Vector2();
            Vector2 botRight = new Vector2();

            while(true)
            {
                Console.WriteLine("--WINDOW CALIBRATION--\n");

                Console.WriteLine("(1) Scale the console to your desired size\n");
                Console.WriteLine("(2) Move your mouse to the position of the very top-left character of the console.");
                Console.WriteLine("This is the first position in the console where your mouse cursor can highlight text.");
                Console.WriteLine("Hover your mouse there and hit Enter to continue.");
                Console.ReadLine();
                GetCursorPos(out mousePos);
                //Mouse position of top left border of console
                topLeft = new Vector2(mousePos.X, mousePos.Y);

                Console.WriteLine("(3) Move your mouse to the position of the very bottom-right character of the console.");
                Console.WriteLine("This is the last position in the console where your mouse cursor can highlight text.");
                Console.WriteLine("Hover your mouse there and hit Enter to continue.");
                Console.ReadLine();
                GetCursorPos(out mousePos);
                //Mouse position of bottom right border of console
                botRight = new Vector2(mousePos.X, mousePos.Y);

                Console.Write("Would you like to recalibrate? (y/n): ");
                var recal = Console.ReadLine();
                if (recal != null && recal.ToUpper().Trim() != "Y")
                {
                    break;
                }
            }

            Console.WriteLine("\n--WINDOW CALIBRATED--\n");

            int ropeSize = 0;

            while(true)
            {
                //Ask user what size rope they would like to use
                Console.WriteLine("What size rope would you like to use?");
                Console.WriteLine("1. Small");
                Console.WriteLine("2. Medium");
                Console.WriteLine("3. Large\n");
                try
                {
                    var sizeString = Console.ReadLine();
                    if (sizeString != null)
                    {
                        ropeSize = Int32.Parse(sizeString.Trim());
                    }
                }
                catch
                {
                    Console.WriteLine("\nPlease enter 1, 2, or 3\n");
                    continue;
                }

                if (ropeSize == 1 || ropeSize == 2 || ropeSize == 3) 
                {
                    break;
                }
                else
                {
                    Console.WriteLine("\nPlease enter 1, 2, or 3\n");
                }
            }

            Console.WriteLine("\nPress Enter to begin!");
            Console.ReadLine();

            //Initialize variables with user specifications
            int N;  //Number of points in rope, greater N == longer rope
            float gravity;  //Acceleration of gravity, higher value for longer rope to increase realism

            //Set values for small rope
            //Gravity: Found a gravity value relative to N=100 that felt right and multiplied 
            //that by the proportion cubed of the other N values
            if (ropeSize == 1)
            {
                N = 100;
                gravity = -9.81f;
            }
            //Set values for medium rope
            else if (ropeSize == 2)
            {
                N = 250;
                gravity = (2.5f*2.5f)*(-9.81f);
            }
            //Set values for large rope
            else
            {
                N = 400;
                gravity = (4*4)*(-9.81f);
            }

            //Store the number of characters in the X and Y axes in the console 
            int consoleX = Console.WindowWidth;
            int consoleY = Console.WindowHeight;

            //Proportion to divide mouse position by to obtain character position
            //width of the screen in mousePos / width of the screen in characters
            float xProp = ((float)(botRight.X - topLeft.X))/consoleX;
            float yProp = ((float)(botRight.Y - topLeft.Y))/consoleY;
        
            int fps = 90; //fps for animation

            int jakobIter = 80; //number of iterations for jakobsen method
            
            float d = 0.75f; //fixed length for rope segments. d < 1 to prevent gaps when printing to console as ints

            //create new array of Point3D objects (x, y, z)
            Point3D[] points = new Point3D[N];
            Point2D[] points2D = new Point2D[N];

            //Initialize pos, divide i/1.5 to give a little bit of slack to the rope
            for (int i = 0; i < N; i++)
            {
                points[i] = new Point3D(i/1.5f, 0, 0);
            }

            //Start watch to calculate frame time
            watch.Start();
            //Infinite simulation
            while(true)
            {
                //Get mouse position coordinates in terms of the console
                GetCursorPos(out mousePos);
                //Convert mouse coordinates to proper position in the console
                float mouseX = (((float)(mousePos.X-topLeft.X))/xProp - Console.WindowWidth/2)/2;    //Subtract half console width to correct for shift in printRope()
                float mouseY = ((consoleY - ((float)(mousePos.Y-topLeft.Y))/yProp) - Console.WindowHeight/2)/2;
                //Set mouse coordinates in 3D (plane of projection at x = 0, so can set 1 to 1)
                points[0].setPos(0, mouseX, mouseY);

                //Verlet Integration
                verlet(points, gravity);

                //Apply constraints with Jakobsen method
                jakobsen(points, d, jakobIter);

                //Project 3D coordinates onto 2D plane
                project3DCoords(points, points2D);

                //Print rope if the frametime has passed based on fps
                if (watch.ElapsedMilliseconds > (float)1000/fps)
                {
                    printRope(points2D);
                    watch.Restart();
                }
            }

        }

        /*
        Verlet Integration:
        Method used to predict the movement of particles 
        given their current acceleration and previous position

        pos(t + dt) = 2 * pos(t) - pos(t - dt) + (dt^2) * a(t)

        Pseudocode:
        function simulateVerlet(Δ𝑡, particles)
            for all particles p do
                positionCopy ← p.position
                p.position ← p.position − p.previousPosition + Δ𝑡^2 * p.getAcceleration()
                p.previousPosition ← positionCopy

        Where p.getAcceleration() is equal to gravity, 9.81m/s^2
        */
        public static void verlet(Point3D[] points, float gravity)
        {
            int N = points.Length;
            float dt = (float)1/200;
            float damping = 0.9925f;

            for (int i = 1; i < N-1; i++)
            {
                //Acceleration is 0 for X and Y, so a(t)*dt^2 cancels out
                float x = points[i].getPos().X, y = points[i].getPos().Y, z = points[i].getPos().Z;

                //Declare and initialize tempPos to hold pos after pos is updated
                Vector3 tempPos = new Vector3(x, y, z);

                //Calculate new x, y, and z
                x += (x - points[i].getPrevPos().X) * damping;
                y += (y - points[i].getPrevPos().Y) * damping;
                z += (z - points[i].getPrevPos().Z) * damping + dt * dt * gravity;

                //Update position
                points[i].setPos(x, y, z);

                //Update previous position
                points[i].setPrevPos(tempPos.X, tempPos.Y, tempPos.Z);
            }
        }

        /*

        Jakobsen Method for enforcing constraints:
        The Jakobsen Method is a way to enforce a constraint
        on the fixed length of rope segments.

        For n iterations, the Jakobsen method calculates the distance
        between two nodes and finds its difference with d, the desired length
        Then it moves the points either a little closer or a little farther apart
        to approach the desired length

        Pseudocode:
        function jakobsen(p1, p2, d)
            delta <- p2.position - p1.position
            distance <- sqrt(delta*delta)
            diff <- (distance - d)/distance
            p1 -= delta*0.5*diff
            p2 += delta*0.5*diff
        */
        public static void jakobsen(Point3D[] points, float d, int jakobIter)
        {
            //Number of points
            int N = points.Length;
            //Variable for storing current position being updated
            Vector3 pos;
            for (int j = 0; j < jakobIter; j++)
            {
                for (int i = 0; i < N-1; i++)
                {
                    //Set pos to current point's position
                    pos = points[i].getPos();

                    //Find difference between point i and point i+1
                    float deltaX = points[i+1].getPos().X - points[i].getPos().X;
                    float deltaY = points[i+1].getPos().Y - points[i].getPos().Y;
                    float deltaZ = points[i+1].getPos().Z - points[i].getPos().Z;
                    //Console.WriteLine($"deltaX: {deltaX}, deltaY: {deltaY}, deltaZ: {deltaZ}");

                    //Calculate distance between points
                    float dist = Vector3.Distance(points[i].getPos(), points[i+1].getPos());
                    //Console.WriteLine($"dist: {dist}");

                    //Calculate the proportion the points are deformed from their proper length
                    float diff = (d - dist)/dist;

                    //Don't move the last fixed node, have
                    //the second to last node do all the moving
                    if (i == N-2)
                    {
                        //Adjust second to last node
                        pos -= new Vector3(deltaX*diff, deltaY*diff, deltaZ*diff);
                        points[i].setPos(pos.X, pos.Y, pos.Z);
                    }
                    //Also don't move the node attached to the cursor
                    //Have the second node do all the moving
                    else if (i == 0)
                    {
                        //Adjust second node
                        pos += new Vector3(deltaX*diff, deltaY*diff, deltaZ*diff);
                        points[i+1].setPos(pos.X, pos.Y, pos.Z);
                    }
                    else
                    {
                        //Update current point
                        pos -= new Vector3(deltaX*0.5f*diff, deltaY*0.5f*diff, deltaZ*0.5f*diff);
                        points[i].setPos(pos.X, pos.Y, pos.Z);
                        //Update next point
                        pos = points[i+1].getPos();
                        pos += new Vector3(deltaX*0.5f*diff, deltaY*0.5f*diff, deltaZ*0.5f*diff);
                        points[i+1].setPos(pos.X, pos.Y, pos.Z);
                    }
                }
            }
        }

        /*
        Returns coordinates for the rope's points
        Set a "camera" at a fixed point that views the 2D projection of the rope
        From the camera, calculate the slope to each of the lines, then find the equation
        Calculate where these lines intersect some plane at x = n

        Derivation of equations:

            Equation of line = (x0, y0, z0) + t<a, b, c>
                - Moving from some origin point (x0, y0, z0), scales the line
                in the direction of vector <a, b, c> by some constant t

            Camera at (xC, yC, zC)
                - Camera must be at the center of the rope and scale
                it's distance away so the rope does not warp
                - xC = length + ropeLength * 1.5
                    - Sets camera (ropeLength*1.5) units away from the front of the rope
                - yC = zC = 0
                    - Sets y and z to the center of the rope

            Point at (x0, y0, z0)
            Screen at plane x = 0 (front of the rope)
            Screen coordinates at (xS, yS)

            Calculating vector from point to camera:
                a = xC - x0
                b = yC - y0
                c = zC - z0

            Equation of line between vertex and camera:

                Line(vertex -> camera) = (x0, y0, z0) + t<a, b, c>

                Now, find where this line intersects the plane x = 0
                    - Plane at the front of the cube 
                    - (xC - length * 1.5 = length + space + length*1.5-length*1.5) = length + space

                xS = 0
                xS = x0 + ta
                x0 + ta = 0
                Therefore,
                t = -x0/a

            The coordinates displayed on screen use the 3D y-coordinates as X and the 3D z-coordinates as Y (looking at cube
            in the direction of -x) derived from the line equation:
                - screen(xS, yS) = (y0 + tb, z0 + tc)
        */
        public static Point2D[] project3DCoords(Point3D[] points, Point2D[] points2D)
        {  
            //Number of points
            int N = points.Length;
            //yScale for adjusting non-square character box (box too tall, so shrink y)
            float yScale = 1f;
            //Scale for adjusting size of rope
            float ropeScale = 2;
            //Camera coordinates, set a bit moved back from origin to give more scale to animation
            //Plane of projection located at plane x=0
            float xCam = -N/2, yCam = 0, zCam = 0;
            //Rope coordinates
            float x0, y0, z0;
            //Vector values to store the vector direction from the point to the camera
            float a, b, c;
            //Constant used to scale vector to meet projection plane
            float t;
            //Rope 2D coordinates
            int x2D, y2D;

            //For each point
            for (int i = 0; i < N; i++)
            {
                //Set x0,y0,z0 to represent rope point
                x0 = points[i].getPos().X;
                y0 = points[i].getPos().Y;
                z0 = points[i].getPos().Z;
                //Set a, b, c vector values to change in x, y, and z, respectively
                a = xCam - x0;
                b = yCam - y0;
                c = zCam - z0;
                //Solve for t
                t = -x0/a;
                //Calculate 2D coordinates
                x2D = (int)((y0 + t*b) * ropeScale);
                y2D = (int)((z0 + t*c) * ropeScale * yScale);
                //Set 2D coordinates
                points2D[i] = new Point2D(x2D, y2D, x0);
            }
            
            //Return 2D array
            return points2D;
        }

        public static void printRope(Point2D[] points2D)
        {
            //Get current window size so user can zoom
            int yMax = Console.WindowHeight;
            int xMax = Console.WindowWidth;
            //Number of points
            int N = points2D.Length;
            //Distance rope shifted over in console for centering
            int shift = xMax/2;

            //Clear the console
            Console.Clear();

            //Calculate X-value of furthest fixed point to calculate characters to print
            float fixedX3D = points2D[N-1].getX3D();

            //Sort points by y descending then x ascending (top to bottom, left to right)
            Array.Sort(points2D);

            //Printing by setting cursor position in console and printing each point individually rather than spacing
            for (int i = 0; i < N; i++)
            {
                //Set cursor position to current point's coords
                //If cursor position is outside of the console, just skip the point
                int cursorX = shift + (int)points2D[i].getPos().X;
                int cursorY = yMax/2 - (int)points2D[i].getPos().Y;

                //If the point is outside of the y-range or x-range, continue
                if (cursorY < 0 || cursorY >= yMax || cursorX < 0 || cursorX >= xMax)
                {
                    i++;
                    continue;
                }

                //Set cursor position to the point in the console
                Console.SetCursorPosition(cursorX, cursorY);

                //Print point character based on X3D distance (distance from camera)
                //Rope broken into fourths, front 1/4th = O, second 1/4th = o, third 1/4th = *, last 1/4th = .
                if (points2D[i].getX3D() < fixedX3D/5)
                {
                    Console.Write("O");
                }
                else if (points2D[i].getX3D() < 2*fixedX3D/5)
                {
                    Console.Write("o");
                }
                else if (points2D[i].getX3D() < 3*fixedX3D/5)
                {
                    Console.Write("*");
                }
                else if (points2D[i].getX3D() < 4*fixedX3D/5)
                {
                    Console.Write("\"");
                }
                else
                {
                    Console.Write(".");
                }

                //If the next points shares the same coordinates, skip it
                while (i != N-1 && (int)points2D[i].getPos().X == (int)points2D[i+1].getPos().X && (int)points2D[i].getPos().Y == (int)points2D[i+1].getPos().Y)
                {
                    i++;
                }
            }
        }
    }
}