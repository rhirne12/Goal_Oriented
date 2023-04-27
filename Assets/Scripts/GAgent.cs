using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor.Search;
using Unity.VisualScripting;

public class SubGoal
{
    public Dictionary<string, int> sgoals;
    public bool remove;

    public SubGoal(string s, int i, bool r)
    {
        sgoals = new Dictionary<string, int>();
        sgoals.Add(s, i);
        remove = r;
    }
}
public class GAgent : MonoBehaviour
{
    public List<GAction> actions = new List<GAction>();
    public Dictionary<SubGoal, int> goals = new Dictionary<SubGoal, int>();
    public GInventory inventory = new GInventory();
    public WorldStates beliefs = new WorldStates();
    

    GPlanner planner;
    Queue<GAction> actionQueue;
    public GAction currentAction;
    SubGoal currentGoal;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        GAction[] acts = this.GetComponents<GAction>();
        foreach (GAction a in acts)
        {
            actions.Add(a);
        }
    }

    bool invoked = false;

    void CompleteAction()
    {
        // reset the planning system so it can perform another action
        currentAction.running = false;  
        currentAction.PostPerform();
        invoked = false;
    }

    void LateUpdate()
    {
        // If we are in the middle of performing an action
        if (currentAction != null && currentAction.running)
        {
            // check that the agents has a goal and has reaced that goal
            float distanceToTarget = Vector3.Distance(currentAction.target.transform.position, this.transform.position);
            if (currentAction.agent.hasPath && distanceToTarget < 2.0f) // currentAction.agent.remainingDistance < 1.0f)
            {
                if (!invoked)
                {
                    Invoke("CompleteAction", currentAction.duration);
                    invoked = true;
                }
            }
            return;
        }
        
        //  Check if we have a planner and an actionQueue
        if (planner == null || actionQueue == null)
        {
            // if planner is null create a new one
            planner = new GPlanner();
            var sortedGoals = from entry in goals orderby entry.Value descending select entry;

            foreach (KeyValuePair<SubGoal, int> sg in sortedGoals)
            {
                actionQueue = planner.plan(actions, sg.Key.sgoals, beliefs);
                if (actionQueue != null)
                {
                    currentGoal = sg.Key;
                    break;
                }
            }
        }

        if (actionQueue != null && actionQueue.Count == 0)  //  Still actions available but queue is empty
        {
            if (currentGoal.remove)  // if gaol is a removalable goal - remove it
            {
                goals.Remove(currentGoal);
            }
            planner = null;  // resets planner and trigger getting another planner
        }

        if (actionQueue != null && actionQueue.Count > 0)  // still actions in queue from planner
        {
            currentAction = actionQueue.Dequeue();   // removes top action off queue and place in current action
            if (currentAction.PrePerform())
            {
                if (currentAction.target ==  null && currentAction.targetTag != "")         //  sets target for agent to move to
                    currentAction.target = GameObject.FindWithTag(currentAction.targetTag);

                if (currentAction.target != null)   // checks to see if there is still actions to move to
                {
                    currentAction.running = true;
                    currentAction.agent.SetDestination(currentAction.target.transform.position);
                }
            }
            else
            {
               actionQueue = null;    // force a new plan retrieval
            }
        }
    }
   
}
