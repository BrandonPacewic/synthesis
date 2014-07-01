﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Inventor;

public class CustomRigidResults
{
    public List<CustomRigidGroup> groups = new List<CustomRigidGroup>();
    public List<CustomRigidJoint> joints = new List<CustomRigidJoint>();

    public Dictionary<string, CustomRigidGroup> groupIDToCustom = new Dictionary<string, CustomRigidGroup>();
    public CustomRigidResults(RigidBodyResults results)
    {
        Console.WriteLine("Building custom dataset");
        foreach (RigidBodyGroup group in results.RigidBodyGroups)
        {
            CustomRigidGroup tmp = new CustomRigidGroup(group);
            if ((!(groupIDToCustom.ContainsKey(CustomRigidGroup.GetGroupQualifier(group)))))
            {
                groups.Add(tmp);
                groupIDToCustom.Add(CustomRigidGroup.GetGroupQualifier(group), tmp);
            }
            else
            {
                Console.WriteLine("GroupID Collision: " + groupIDToCustom[CustomRigidGroup.GetGroupQualifier(group)].ToString() + " and " + tmp.ToString());
            }
        }
        foreach (RigidBodyJoint joint in results.RigidBodyJoints)
        {
            joints.Add(new CustomRigidJoint(joint, groupIDToCustom[CustomRigidGroup.GetGroupQualifier(joint.GroupOne)], groupIDToCustom[CustomRigidGroup.GetGroupQualifier(joint.GroupTwo)]));
        }
        Console.WriteLine("Built custom dataset");
        RigidBodyCleaner.CleanMeaningless(this);
    }
}

public class CustomRigidGroup
{
    public List<ComponentOccurrence> occurrences = new List<ComponentOccurrence>();

    public bool grounded;

    public bool highRes = false;
    public bool colorFaces = false;

    public string fullQualifier;
    public static string GetGroupQualifier(RigidBodyGroup group)
    {
        return group.GroupID + "_" + group.Parent.Parent.Parent.Parent.InternalName;
    }

    public CustomRigidGroup(RigidBodyGroup group)
    {
        foreach (ComponentOccurrence comp in group.Occurrences)
        {
            occurrences.Add(comp);
        }
        grounded = group.Grounded;
        fullQualifier = GetGroupQualifier(group);
    }

    public override string ToString()
    {
        if (occurrences.Count == 1)
        {
            return occurrences[0].Name;
        }
        string res = "[";
        foreach (ComponentOccurrence occ in occurrences)
        {
            if (res.Length > 100)
            {
                res += "...";
                break; // TODO: might not be correct. Was : Exit For
            }
            res += occ.Name + ";";
        }
        res += "]";
        return res;
    }

    public override bool Equals(object obj)
    {
        if ((obj is CustomRigidGroup))
        {
            return fullQualifier.Equals(((CustomRigidGroup)obj).fullQualifier);
        }
        else if ((obj is RigidBodyGroup))
        {
            return fullQualifier.Equals(GetGroupQualifier((RigidBodyGroup)obj));
        }
        else
        {
            return false;
        }
    }

    public override int GetHashCode()
    {
        return fullQualifier.GetHashCode();
    }
}

public class CustomRigidJoint
{
    public List<AssemblyJoint> joints = new List<AssemblyJoint>();
    public List<AssemblyConstraint> constraints = new List<AssemblyConstraint>();
    public CustomRigidGroup groupOne;
    public CustomRigidGroup groupTwo;

    public RigidBodyJointTypeEnum type;

    public SkeletalJoint skeletalJoint = null;
    public CustomRigidJoint(RigidBodyJoint joint, CustomRigidGroup groupOnez, CustomRigidGroup groupTwoz)
    {
        foreach (AssemblyJoint aj in joint.Joints)
        {
            joints.Add(aj);
        }
        foreach (AssemblyConstraint cj in joint.Constraints)
        {
            constraints.Add(cj);
        }
        groupOne = groupOnez;
        groupTwo = groupTwoz;
        type = joint.JointType;
    }

    public override string ToString()
    {
        return "RigidJoint (" + Enum.GetName(typeof(RigidBodyJointTypeEnum), type) + "): " + constraints.Count + "C, " + joints.Count + "J";
    }
}