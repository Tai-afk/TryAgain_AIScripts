using System.Collections.Generic;
using UnityEngine;

public class BaseAttack : AttackAction
{
    public BaseAttack(Transform transform, AttackParams attackParams, LayerMask layerMask)
        : base(transform, attackParams, layerMask) {}

    public override bool Check()
    {
	    float startRange = attackParams.startRange,
		    landDistance = attackParams.landDistance,
		    traceRadius = attackParams.traceRadius;
	    int maxTraces = attackParams.maxTraces;
	    bool drawTraces = attackParams.drawTraces;
	    float distance = Vector3.Distance(transform.position, transform.GetComponent<AIStateControl>().player.transform.position);

	    if (distance > startRange)
	    {
		    return false;
	    }
	    
        if (maxTraces <= 0)
		{
			Debug.Log("Invalid climb check configuration.");
			return false;
		}
        
		// Perform raycasts ahead of the player from bottom to top to find an obstacle in front of them.
		/*for (int traceIdx = 0; traceIdx < maxTraces; traceIdx++)
		{
			Vector3 obstacleCheckPos = obstacleCheckBasePos + transform.up * (obstacleHeightCheckDelta * traceIdx);
			bool hit = Physics.SphereCast(obstacleCheckPos, traceRadius, transform.forward,
										  out RaycastHit obstacleHit, startRange, traceMask, QueryTriggerInteraction.Ignore);
			if (drawTraces)
			{
				Debug.DrawRay(obstacleCheckPos, transform.forward * startRange, Color.blue, 3);
			}

			if (hit)
			{
				if (checkDisableTags && CheckObstacleDisabled(obstacleHit, disableTags))
				{
					return false;
				}
				
				// Check that there is space above the obstacle for the player to vault over.
				Vector3 clearanceCheckPos = obstacleCheckPos + transform.up * heightClearance;
				float clearanceDistance = (transform.position - new Vector3(obstacleHit.point.x, transform.position.y, obstacleHit.point.z)).magnitude + 0.1f;
				bool blocked = Physics.SphereCast(clearanceCheckPos, traceRadius, transform.forward,
					out RaycastHit clearanceHit, clearanceDistance, traceMask, QueryTriggerInteraction.Ignore);

				if (drawTraces)
				{
					Debug.DrawRay(clearanceCheckPos, transform.forward * clearanceDistance, Color.yellow, 3);
				}

				if (!blocked)
				{
					// Find a point on top of the obstacle for the player to start climbing from.
					Vector3 obstacleTopCheckPos = obstacleHit.point + transform.up * heightClearance;
					bool foundTop = Physics.SphereCast(obstacleTopCheckPos, traceRadius, -transform.up,
													   out RaycastHit topHit, heightClearance, traceMask, QueryTriggerInteraction.Ignore);
					if (drawTraces)
					{
						Debug.DrawRay(obstacleTopCheckPos, -transform.up * heightClearance, Color.red, 3);
					}

					if (foundTop)
					{
						if (checkDisableTags && CheckObstacleDisabled(topHit, disableTags))
						{
							return false;
						}
						
						// Find a point on the face of the obstacle to grab onto to start climbing.
						float stepDistance = (transform.position - new Vector3(topHit.point.x, transform.position.y, topHit.point.z)).magnitude;
						Vector3 climbStartCheckPos = topHit.point - transform.forward * stepDistance;
						
						foundClimbPoint = Physics.SphereCast(climbStartCheckPos, traceRadius, transform.forward,
															 out climbStartHit, stepDistance + 0.1f, traceMask, QueryTriggerInteraction.Ignore);
						if (drawTraces)
						{
							Debug.DrawRay(climbStartCheckPos, transform.forward * (stepDistance + 0.1f), Color.green, 3);
						}

						if (foundClimbPoint)
						{
							break;
						}
					}
				}
			}
		}

		if (foundClimbPoint)
		{
			Vector3 climbDir = Vector3.ProjectOnPlane(-climbStartHit.normal, Vector3.up);
			Vector3 startPoint = climbStartHit.point, exitPoint = climbStartHit.point;
			Quaternion rotation = Quaternion.LookRotation(climbDir, Vector3.up);
			Vector3 forward = -climbStartHit.normal, right = Vector3.Cross(Vector3.up, -climbStartHit.normal), up = Vector3.up;

			obstacleColliders.Clear();
			obstacleColliders.Add(climbStartHit.collider);

			// Find a point above the obstacle to stand at after climbing
			Vector3 exitCheckPos = startPoint + Vector3.up * heightClearance - climbStartHit.normal * exitDistance;
			bool foundExit = Physics.SphereCast(exitCheckPos, traceRadius, -Vector3.up, out RaycastHit exitHit,
												heightClearance + 0.1f, traceMask, QueryTriggerInteraction.Ignore);
			if (drawTraces)
			{
				Debug.DrawRay(exitCheckPos, -Vector3.up * (heightClearance + 0.1f), Color.magenta, 3);
			}

			if (foundExit)
			{
				if (checkDisableTags && CheckObstacleDisabled(exitHit, disableTags))
				{
					return false;
				}
				
				exitPoint = exitHit.point;
				
				targetMatchingParams.Clear();
				if (numSteps > 0)
				{
					Vector3 stepBlockCheckPos = transform.position + transform.up * stepCheckHeight;
					bool foundBlock = Physics.SphereCast(stepBlockCheckPos, traceRadius, transform.forward, out RaycastHit blockHit,
						startRange, traceMask, QueryTriggerInteraction.Ignore);
					if (drawTraces)
					{
						Debug.DrawRay(stepBlockCheckPos, transform.forward * startRange, Color.white, 3);
					}
					
					if (foundBlock)
					{
						if (checkDisableTags && CheckObstacleDisabled(blockHit, disableTags))
						{
							return false;
						}
						
						float horizBlockDist = (startPoint - new Vector3(blockHit.point.x, startPoint.y, blockHit.point.z)).magnitude;
						if (horizBlockDist > maxStepDistance)
						{
							return false;
						}
					}

					// Get the points along the wall to step onto to clamber up to the top
					Vector3 basePos = new Vector3(startPoint.x, transform.position.y, startPoint.z);
					float stepDistance = (transform.position - basePos).magnitude;

					for (int stepIdx = 1; stepIdx <= numSteps; stepIdx++)
					{
						float t = stepIdx / (numSteps + 1);
						Vector3 stepPos = Vector3.Lerp(basePos, startPoint, t);
						Vector3 stepRayStartPos = stepPos - transform.forward * stepDistance;
						
						bool foundStep = Physics.SphereCast(stepRayStartPos, traceRadius, transform.forward, out RaycastHit stepHit,
															stepDistance + 0.1f, traceMask, QueryTriggerInteraction.Ignore);
						if (drawTraces)
						{
							Debug.DrawRay(stepRayStartPos, transform.forward * (stepDistance + 0.1f), Color.cyan, 3);
						}
						
						if (foundStep)
						{
							if (checkDisableTags && CheckObstacleDisabled(stepHit, disableTags))
							{
								return false;
							}
							
							float horizStepDist = (startPoint - new Vector3(stepHit.point.x, startPoint.y, stepHit.point.z)).magnitude;
							if (horizStepDist > maxStepDistance)
							{
								return false;
							}
							
							targetMatchingParams.Add(new TargetMatchParams()
							{
								targetPosition = stepHit.point,
								targetRotation = rotation,
								forward = forward,
								right = right,
								up = up
							});
						}
						else
						{
							return false;
						}
					}
				}

				targetMatchingParams.Add(new TargetMatchParams()
				{
					targetPosition = startPoint,
					targetRotation = rotation,
					forward = forward,
					right = right,
					up = up
				});

				targetMatchingParams.Add(new TargetMatchParams()
				{
					targetPosition = exitPoint,
					targetRotation = rotation,
					forward = forward,
					right = right,
					up = up
				});
				
				return true;
			}
		}
		*/

		return false;
	    
    }

    protected bool CheckObstacleDisabled(RaycastHit hit, List<string> disableTags)
    {
	    CustomTag tagComponent = hit.collider.GetComponent<CustomTag>();
	    if (tagComponent)
	    {
		    foreach (string tag in disableTags)
		    {
			    if (tagComponent.HasTag(tag))
			    {
				    return true;
			    }
		    }
	    }

	    return false;
    }
}
