using System.Collections;
using System.Collections.Generic;

using System;
using System.Linq;
using UnityEngine;

using Unity.MLAgents.Policies;
using Unity.MLAgents;

public class MapRangeOfMotion2Constraints : MonoBehaviour
{

    /*
    [SerializeField]
    bool applyROMInGamePlay;

    public bool ApplyROMInGamePlay {  set => applyROMInGamePlay = value; }
    */


    public RangeOfMotionValues info2store;

    [Range(0, 359)]
    int MinROMNeededForJoint = 5;




    [SerializeField]
    bool debugWithLargestROM = false;




    public void ConfigureTrainingForRagdoll(int minROM)
    {

        MinROMNeededForJoint = minROM;

        int dof = ApplyRangeOfMotionToRagDoll();
        if (dof == -1)
        {
            Debug.LogError("Problems applying the range of motion to the ragdoll");
        }
        else
        {

            ApplyDoFOnBehaviorParameters(dof);
        }

    }


    int ApplyRangeOfMotionToRagDoll()
    {
        if (info2store == null || info2store.Values.Length == 0)
            return -1;

        ArticulationBody[] articulationBodies = 
            GetComponentsInChildren<ArticulationBody>(true)
            .Where(x=>x.name.StartsWith("articulation:"))
            .ToArray();

        int DegreesOfFreedom = 0;

        foreach (ArticulationBody body in articulationBodies)
        {
            string keyword1 = "articulation:";
            string keyword2 = "collider:";
            string valuename = body.name.TrimStart(keyword1.ToArray<char>());
            valuename = valuename.TrimStart(keyword2.ToArray<char>());

            RangeOfMotionValue rom = info2store.Values.FirstOrDefault(x => x.name == valuename);

            if (rom == null)
            {
                Debug.LogError("Could not find a rangoe of motionvalue for articulation: " + body.name);
                return -1;
            }

            bool isLocked = true;
            body.twistLock = ArticulationDofLock.LockedMotion;
            body.swingYLock = ArticulationDofLock.LockedMotion;
            body.swingZLock = ArticulationDofLock.LockedMotion;
            body.jointType = ArticulationJointType.FixedJoint;

            body.anchorRotation = Quaternion.identity; //we make sure the anchor has no Rotation, otherwise the constraints do not make any sense

            if (rom.rangeOfMotion.x > (float)MinROMNeededForJoint)
            {
                DegreesOfFreedom++;
                isLocked = false;
                body.twistLock = ArticulationDofLock.LimitedMotion;
                var drive = body.xDrive;
                drive.lowerLimit = rom.lower.x;
                drive.upperLimit = rom.upper.x;
                body.xDrive = drive;
                if (debugWithLargestROM)
                {
                    drive.lowerLimit = -170;
                    drive.upperLimit = +170;
                }

            }
            if (rom.rangeOfMotion.y >= (float)MinROMNeededForJoint)
            {
                DegreesOfFreedom++;
                isLocked = false;
                body.swingYLock = ArticulationDofLock.LimitedMotion;
                var drive = body.yDrive;
                drive.lowerLimit = rom.lower.y;
                drive.upperLimit = rom.upper.y;
                body.yDrive = drive;

                if (debugWithLargestROM)
                {
                    drive.lowerLimit = -170;
                    drive.upperLimit = +170;
                }


            }
            if (rom.rangeOfMotion.z >= (float)MinROMNeededForJoint)
            {
                DegreesOfFreedom++;
                isLocked = false;
                body.swingZLock = ArticulationDofLock.LimitedMotion;
                var drive = body.zDrive;
                drive.lowerLimit = rom.lower.z;
                drive.upperLimit = rom.upper.z;
                body.zDrive = drive;

                if (debugWithLargestROM)
                {
                    drive.lowerLimit = -170;
                    drive.upperLimit = +170;
                }

            }

            if (!isLocked)
            {
                body.jointType = ArticulationJointType.SphericalJoint;
            }

        }

        return DegreesOfFreedom;

    }





    void ApplyDoFOnBehaviorParameters(int DegreesOfFreedom)
    {
        // due to an obscure function call in the setter of ActionSpec inside bp, this can only run at runtime
        //the function is called SyncDeprecatedActionFields()


        BehaviorParameters bp = GetComponent<BehaviorParameters>();

        Unity.MLAgents.Actuators.ActionSpec myActionSpec = bp.BrainParameters.ActionSpec;



        myActionSpec.NumContinuousActions = DegreesOfFreedom;
        myActionSpec.BranchSizes = new List<int>().ToArray();
        bp.BrainParameters.ActionSpec = myActionSpec;
        Debug.Log("Space of actions calculated at:" + myActionSpec.NumContinuousActions + " continuous dimensions");


        /*
         * To calculate the space of observations, apparently the formula is:
        number of colliders(19) *12 +number of actions(54) + number of sensors(6) + misc addition observations(16) = 304
        correction: it seeems to be:
        number of actions + number of sensors + misc additional observations = 101
        
        */

        //int numcolliders = GetComponentsInChildren<CapsuleCollider>().Length; //notice the sensors are Spherecolliders, so not included in this count
        int numsensors = GetComponentsInChildren<SensorBehavior>().Length;
        int num_miscelaneous = GetComponent<ProcRagdollAgent>().calculateDreConObservationsize();

        int ObservationDimensions = DegreesOfFreedom + numsensors + num_miscelaneous;
        bp.BrainParameters.VectorObservationSize = ObservationDimensions;
        Debug.Log("Space of perceptions calculated at:" + bp.BrainParameters.VectorObservationSize + " continuous dimensions, with: " + "sensors: " + numsensors + "and DreCon miscelaneous: " + num_miscelaneous);


    }



}
