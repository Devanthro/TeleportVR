/* 
 * This message is auto generated by ROS#. Please DO NOT modify.
 * Note:
 * - Comments from the original code will be written in their own line 
 * - Variable sized arrays will be initialized to array of size 0 
 * Please report any issues at 
 * <https://github.com/siemens/ros-sharp> 
 */



using RosSharp.RosBridgeClient.MessageTypes.Geometry;

namespace RosSharp.RosBridgeClient.MessageTypes.RoboySimulation
{
    public class ContactPoint : Message
    {
        public const string RosMessageName = "roboy_simulation_msgs/msg/ContactPoint";

        // id of roboy link in which a collision happened
        public long linkid { get; set; }
        // position of contact point on roboy in link frame
        public Vector3 position { get; set; }
        // contact normal on external body, pointing towards roboy
        public Vector3 contactnormal { get; set; }
        // contact distance, positive for separation, negative for penetration
        public double contactdistance { get; set; }
        // normal force applied during the last 'stepSimulation'
        public double normalforce { get; set; }

        public ContactPoint()
        {
            this.linkid = 0;
            this.position = new Vector3();
            this.contactnormal = new Vector3();
            this.contactdistance = 0.0;
            this.normalforce = 0.0;
        }

        public ContactPoint(long linkid, Vector3 position, Vector3 contactnormal, double contactdistance, double normalforce)
        {
            this.linkid = linkid;
            this.position = position;
            this.contactnormal = contactnormal;
            this.contactdistance = contactdistance;
            this.normalforce = normalforce;
        }
    }
}
