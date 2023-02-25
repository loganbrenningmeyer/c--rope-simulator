using System.Numerics;

namespace Rope
{
    /*
    Point2D:
    Holds the (x, y) coordinates of projected points for printing to the console
    Also stores the original 3D x-coordinate of the point to determine character size
    */
    class Point2D : IComparable<Point2D>
    {
        //Vector2 of 2D position (x,y)
        private Vector2 pos2D;
        //x3D stores the original 3D x-value to determine size of character
        //when printing
        private float x3D;

        public int CompareTo(Point2D? other)
        {
            if (other == null)
            {
                return 1;
            }
            //If Y's are not equal, sort by Y descending (comparing backwards with other first)
            if (this.pos2D.Y != other.pos2D.Y)
            {
                return other.pos2D.Y.CompareTo(this.pos2D.Y);
            }
            else
            {
                //If X's are different, sort by X ascending
                if (this.pos2D.X != other.pos2D.X)
                {
                    return this.pos2D.X.CompareTo(other.pos2D.X);
                }
                //If X's are equal, sort by X3D ascending so closer points have precedence
                else
                {
                    return this.x3D.CompareTo(other.x3D);
                }
            }
        }

        public Point2D(int x, int y, float x3D)
        {
            this.pos2D = new Vector2(x, y);
            this.x3D = x3D;
        }

        public Vector2 getPos()
        {
            return this.pos2D;
        }

        public float getX3D()
        {
            return this.x3D;
        }
    }
}