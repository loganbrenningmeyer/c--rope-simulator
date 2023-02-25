using System.Numerics;

namespace Rope
{
    /*
    Point:
    Holds the (x, y, z) coordinates of points as well as their previous position
    */
    class Point3D
    {

        private Vector3 prevPos;
        private Vector3 pos;

        public Point3D(float x, float y, float z)
        {
            this.prevPos = new Vector3(x, y, z);
            this.pos = new Vector3(x, y, z);
        }

        public void setPrevPos(float x, float y, float z)
        {
            this.prevPos.X = x;
            this.prevPos.Y = y;
            this.prevPos.Z = z;
        }

        public void setPos(float x, float y, float z)
        {
            this.pos.X = x;
            this.pos.Y = y;
            this.pos.Z = z;
        }

        public Vector3 getPrevPos()
        {
            return this.prevPos;
        }

        public Vector3 getPos()
        {
            return this.pos;
        }   
    }
}