class SurvivorBotDoMove
{
    protected int m_Speed = 0;
    protected bool m_OverrideSpeed = false;
    protected bool m_OverrideMove = false;
    protected bool m_OverrideFinal = false;
	protected bool m_OverrideWalk = false;
	protected bool s_Idle;
	

	AIWorld world = GetGame().GetWorld().GetAIWorld();
	ref PGFilter m_pgFilter = new PGFilter();

    protected float m_TargetDistance;
    protected int m_CurrentPosition;

	protected vector m_TargetPosition;
	protected vector m_TargetDirection;

    protected PlayerBase m_Player;
    protected SurvivorBotBase m_Soldier;
	protected bool IsNavmesh;
	protected  float m_distToTargetPos;
	
	ref TVectorArray waypoints = new TVectorArray;
	protected bool IsWaypoints = false;
	protected bool IsUseWaypoints = false;
	protected bool m_dirFixWeap = false;
	
	
	protected bool m_IsCollision;
	protected bool m_DisableTargetMovement;
    protected EntityAI m_NavmehObjectTarget;
    protected int m_CheckpointCount = 0;
	protected int m_LastCheckpointNum = 0;
	
	protected bool OnWaypoint;
	
	protected int m_waypointCount = 0;
	protected int m_WaypointNum = 0;
	
    void SurvivorBotDoMove(PlayerBase m_Owner, SurvivorBotBase m_SoldierOwner) { m_Player = m_Owner; m_Soldier = m_SoldierOwner; OnEntry(); }
    void ~SurvivorBotDoMove() { OnExit(); }

    void OnEntry()
    {
     //   Print("[esbsdm] OnEntry " + this);

        m_TargetDistance = 1;
        m_CurrentPosition = 0;
        m_TargetPosition = "0 0 0";
		m_distToTargetPos = 0;
       
        m_Speed = 0;
        m_OverrideSpeed = false;
        m_OverrideMove = false;
        m_OverrideFinal = false;
		m_OverrideWalk = false;
		IsNavmesh = false;
		OnWaypoint = true;
		
		m_pgFilter.SetCost(PGAreaType.ROADWAY, 0);
		m_pgFilter.SetFlags(PGPolyFlags.WALK, PGPolyFlags.CROUCH, 0);
		
		m_IsCollision = false;
		m_DisableTargetMovement = false;
		
		m_NavmehObjectTarget = EntityAI.Cast(GetGame().CreateObject("test_emtyObj", m_Soldier.GetPosition()));
		
		if (m_Soldier.IsUseCheckpoint())
		{
			m_CheckpointCount = m_Soldier.m_ArrayCheckpoint.Count();
			SendLog("Бот использует чекпоинты! Колличество: " + m_CheckpointCount.ToString());
		}
    }
	
    void EnableTargetMovement()
	{
	   //SendLog("---IsCollision EnableTargetMovement--- m_DisableTargetMovement = false" );
	   SetDirNavmeh(m_TargetPosition);
	   if (m_DisableTargetMovement)
		   m_DisableTargetMovement = false;
	}


   
	void IgnoreObjects()
	{	

	   GetNavmesh(m_Soldier.GetPosition());
	   
		if (m_NavmehObjectTarget)
		{
			float centerX = m_Soldier.GetPosition()[0];
			float centerZ = m_Soldier.GetPosition()[2];

			float angle = m_Soldier.GetOrientation()[0];
			float rads = angle * Math.DEG2RAD;		

			float dX = Math.Sin(rads) * 1;
			float dZ = Math.Cos(rads) * 1;

			float x = centerX + dX;
			float z = centerZ + dZ;
			float y = GetGame().SurfaceY(x,z);

			vector objPos = Vector(x,y,z);
			
			m_NavmehObjectTarget.SetPosition( GetNavmesh(objPos) );
			
		}
		
       if (m_IsCollision)
	   {	
			if (!m_DisableTargetMovement)
			{
				m_DisableTargetMovement = true;
				m_Soldier.GetDoJumpFSM().bot_JumpOrClimb();
				ResetWaypoints();
				
				GetGame().GetCallQueue(CALL_CATEGORY_GAMEPLAY).CallLater(EnableTargetMovement, 3000, false); 
			}
	   }
	}
	
	void SetDirNavmeh(vector m_VectorNav)
	{
		vector direction_vector = vector.Direction(m_VectorNav, m_Soldier.GetPosition()).Normalized() * -1;
		vector direction_no_z = Vector(direction_vector[0],0,direction_vector[2]).Normalized();
		m_Soldier.SetDirection(direction_no_z);	
	}

	void OnExit () 
    {
        Print("[esbsdm] OnExit " + this);
		if (m_Soldier)
		{
			m_Soldier.GetInputController().OverrideMovementSpeed(false, 0); 
			m_Soldier.GetInputController().OverrideMovementAngle(false, 0);
		}

        m_TargetDistance = 0;
        m_CurrentPosition = 0;
        m_TargetPosition = "0 0 0";

        m_Speed = 0;
        m_OverrideSpeed = false;
        m_OverrideMove = false;
        m_OverrideFinal = false;
		IsNavmesh = false;
		if(m_NavmehObjectTarget)
		{
			GetGame().ObjectDelete( m_NavmehObjectTarget );
		}
		
    }

	void OnUpdate (float dt) 
    {
		m_distToTargetPos = vector.Distance(m_Soldier.GetPosition(), m_TargetPosition);

		if(m_distToTargetPos > m_TargetDistance)
		{
			
            if (m_OverrideMove)
            {
				if (m_Soldier.GetCommand_Move() && m_Soldier.IsBot()) 
        		{
					m_Soldier.GetCommand_Move().ForceStanceUp(DayZPlayerConstants.STANCEIDX_ERECT);
            		m_Soldier.GetCommand_Move().ForceStance(DayZPlayerConstants.STANCEIDX_ERECT);
        		}
				
				if (m_Soldier.GetTarget())
				{
					m_TargetPosition = GetNewPoint(m_Soldier.GetTarget().GetPosition());											
					m_Soldier.SetDirection(m_TargetDirection);
				}
				
				bool m_CalcDir = CalculateNewDirection();
				if (m_CalcDir)
				{
					m_Soldier.GetInputController().OverrideMovementSpeed( true, CalculateSpeedMode() ); 
					m_Soldier.GetInputController().OverrideMovementAngle( true, 0 ); 
				//	bool b_isObjInFront = IsObjectInFront(m_Soldier, 0);
				}	

            }
		}
		else
		{
			m_Soldier.GetInputController().OverrideMovementSpeed( false, 0 ); 
			m_Soldier.GetInputController().OverrideMovementAngle( false, 0 );
            if (!m_IsCollision && !m_DisableTargetMovement)
			{

				if (m_Soldier.GetTarget())
				{
					if (m_dirFixWeap)
						SetDirNavmeh(m_Soldier.GetTarget().GetPosition());
					else
						m_TargetPosition = GetNewPoint(m_Soldier.GetTarget().GetPosition());
					
				}
				else
				{
					m_Soldier.SetDirection(m_TargetDirection);
				}
				
			}
	
		}	

		if (!m_Soldier.GetTarget()) 
        {
			SetTarget(m_Soldier.GetBeginPosition(), true, false, 1);
			m_Soldier.SetDirection(m_TargetDirection);			
		}
		
		
		if (m_Soldier.IsUseCheckpoint() && m_Soldier.m_ArrayCheckpoint.Count() != 1 && m_Soldier.m_ArrayCheckpoint.Count() != 0)
			UseCheckpoint();
    }
	

    void UseCheckpoint()
	{
		vector m_Checkpoint;
		float IsCheckpointDist = 3.0;
		if (m_LastCheckpointNum == 0)
		{
			m_LastCheckpointNum ++;
			m_Checkpoint = m_Soldier.m_ArrayCheckpoint.Get(m_LastCheckpointNum);
			m_Soldier.SetBeginPosition(m_Checkpoint);
		//	SendLog("Бот использует чекпоинты! Pos: " + m_Checkpoint.ToString());
		}
        else
		{
			if (!m_Soldier.m_IsVoice)
			{
				m_Soldier.m_IsVoice = true;
				
				GetGame().CreateSoundOnObject(m_Soldier, "Bot_idle_" + Math.RandomInt(1, 32).ToString(), 40, false);
				
				GetGame().GetCallQueue(CALL_CATEGORY_GAMEPLAY).CallLater(m_Soldier.VoiceEnd, 12000, false); 
			}
			
			if (vector.Distance(Vector(m_Soldier.GetBeginPosition()[0], m_Soldier.GetPosition()[1], m_Soldier.GetBeginPosition()[2]), m_Soldier.GetPosition()) < IsCheckpointDist && m_LastCheckpointNum != m_CheckpointCount)
			{
				
				m_LastCheckpointNum ++;
				m_Checkpoint = m_Soldier.m_ArrayCheckpoint.Get(m_LastCheckpointNum);
				m_Soldier.SetBeginPosition(m_Checkpoint);
			//	if(m_LastCheckpointNum == 31)
			//		m_LastCheckpointNum ++;
					
			//	SendLog("Бот достиг чекпоина назначем следуший!" + m_LastCheckpointNum + " Pos: " + m_Checkpoint.ToString());
			}
			else if (m_LastCheckpointNum == m_CheckpointCount)
			{
				m_Soldier.m_ArrayCheckpoint.Invert();
				m_LastCheckpointNum = 0;
			//	SendLog("Бот обошёл все чекоенты Делаем реверс!");
			}
		}
		
	}
	
	void SetTarget(vector Position, bool OverrideMove = true, bool OverrideSpeed = false, int Speed = 1, float Distance = 1)
	{
		
		m_TargetPosition = GetNewPoint(Position);

		m_TargetDistance = Distance;

		m_OverrideSpeed = OverrideSpeed;
		m_OverrideMove = OverrideMove;
		m_Speed = Speed;
		m_dirFixWeap = false;
	} 	
	
	void SetTargetWeap(vector Position, bool OverrideMove = true, bool OverrideSpeed = false, int Speed = 1, float Distance = 1)
	{	
		m_TargetPosition = GetNewPoint(Position);

		m_TargetDistance = Distance;

		m_OverrideSpeed = OverrideSpeed;
		m_OverrideMove = OverrideMove;
		m_Speed = Speed;
		m_dirFixWeap = true;
	} 
	

    void OnSelectPositionLatter()
	{
		if (!m_Soldier.GetTarget()) 
        {
			SetTarget(m_Soldier.GetBeginPosition(), true, false, 1);
		}
	}

	void OnMovement()
	{
		if (m_OverrideWalk)
		{
			GetGame().GetCallQueue(CALL_CATEGORY_SYSTEM).CallLater( OnSelectPositionLatter, 10, false );

			m_OverrideWalk = false;
		}	
	}
	
    void SetStop()
    {
        m_OverrideMove = false;
		m_OverrideWalk = false;
		ResetWaypoints();
    }

    void SetMove()
    {
		m_OverrideMove = true;
		m_OverrideWalk = true;
    }
	
	void generate_WayPoint(vector pos_Target)
	{
		if (OnWaypoint)
		{
			IsUseWaypoints = true;
	
			IsWaypoints = world.FindPath(m_Soldier.GetPosition(), pos_Target, m_pgFilter, waypoints);

		}
		OnWaypoint = false;
	} 
	
	
	vector GetNewPoint(vector pos)
	{		
		if (IsUseWaypoints && IsWaypoints)
		{
				
			m_waypointCount = waypoints.Count();
				
			if (m_WaypointNum == 0)
			{	
				m_WaypointNum ++;	
				return waypoints[m_WaypointNum];
			}
			
			if (vector.Distance( Vector(waypoints[m_WaypointNum][0], m_Soldier.GetPosition()[1], waypoints[m_WaypointNum][2] ), m_Soldier.GetPosition()) < 1.5 && m_WaypointNum != m_waypointCount)
			{
				m_WaypointNum ++;
				return waypoints[m_WaypointNum];
			}
			
			if (m_WaypointNum == m_waypointCount)
			{
				ResetWaypoints();
				return pos;
			}
		}
		generate_WayPoint( pos );
		
		return GetNavmesh( waypoints[m_WaypointNum] );
	}
	
	void ResetWaypoints()
	{
		waypoints.Clear();
		m_WaypointNum = 0;
		m_waypointCount = 0;
		IsUseWaypoints = false;	
		OnWaypoint = true;		
	}
	
    bool CalculateNewDirection()
	{	

		if (m_TargetPosition)
		{			
			vector direction_vector = vector.Direction(m_TargetPosition, m_Soldier.GetPosition()).Normalized() * -1;
			vector direction_no_z = Vector(direction_vector[0],0,direction_vector[2]).Normalized();
			vector bot_dir = m_Soldier.GetDirection().Normalized();
			vector bot_dir_no_z = Vector(bot_dir[0],0,bot_dir[2]).Normalized();

			float direction_angle = Math.Atan2(direction_no_z[0],direction_no_z[2]) * Math.RAD2DEG;
			float bot_angle = Math.Atan2(bot_dir_no_z[0],bot_dir_no_z[2]) * Math.RAD2DEG;

			if (direction_angle < 0)
			{
				direction_angle += 360;
			}
			if (bot_angle < 0)
			{
				bot_angle += 360;
			}

			float deltaDir = direction_angle - bot_angle;

			if (deltaDir > 180)
			{
				deltaDir -= 360;
			}
			if (deltaDir < -180)
			{
				deltaDir += 360;
			}	

			vector new_bot_dir = direction_no_z;
			if (Math.AbsFloat(deltaDir) > 5 )
			{	
				float multi = 5;
				if(deltaDir < 0)
				{
					multi = -1;
				}

				bot_angle += (2.5 * multi);

				float dX = Math.Sin(bot_angle * Math.DEG2RAD);
				float dY = Math.Cos(bot_angle * Math.DEG2RAD);
				new_bot_dir = Vector( dX, 0, dY).Normalized();	
			}
			else
			{
				return true;
			}
			m_TargetDirection = new_bot_dir;
		}
		return false;
	}

	int CalculateSpeedMode()
	{
		if(m_OverrideSpeed && m_Speed > 0)
			return m_Speed;
		float speed;
		float d;
		if (m_Soldier.GetTarget())
		{
			d = vector.Distance(m_Soldier.GetTarget().GetPosition(), m_Soldier.GetPosition());
			speed = 2;
			if(d > 1)
			{
				speed = 2;
				IgnoreObjects();
			}
			if(d > 5)
			{
				speed = 3;
				IgnoreObjects();
				
			}		
		}
		else
		{
			d = vector.Distance(m_TargetPosition, m_Soldier.GetPosition());
			
			speed = 1;
			if(d > 2)
			{
				speed = 2;
				IgnoreObjects();
			}
			if(d > 10)
			{
				speed = 3;
				IgnoreObjects();
			}
		}
		return speed;
	}
	
	
	bool IsObjectInFront(PlayerBase Target, int azimunt)
	{
		if (Target) 
        {
			int headIndex = Target.GetBoneIndexByName("Head");
			
			vector rayStart = Target.GetBonePositionWS(headIndex);
			
			float centerX = Target.GetPosition()[0];
			float centerZ = Target.GetPosition()[2];

			float angle = Target.GetOrientation()[0] + azimunt;
			float rads = angle * Math.DEG2RAD;		

			float dX = Math.Sin(rads) * 1;
			float dZ = Math.Cos(rads) * 1;

			float x = centerX + dX;
			float z = centerZ + dZ;
			float y = GetGame().SurfaceY(x,z);

			vector rayEnd = Vector(x,y,z);
   			auto objs = GetObjectsAt( rayStart, rayEnd, Target );
			
			if (objs)
			{
				if( objs.Count() ) 
				{
					if (objs[0].IsBuilding() || objs[0].IsTree() || objs[0].IsRock() || objs[0].IsWoodBase() ) 
					{	
						m_IsCollision = true;
						
						return true;
					}
					else if (objs[0].IsBush())
					{
						return false;
					}
					IgnoreObjects();
				}
			}
		}
		return false;
	}
	
	static set< Object > GetObjectsAt( vector from, vector to, Object ignore = NULL, float radius = 0.5, Object with = NULL )
	{
		vector contact_pos;
		vector contact_dir;
		int contact_component;

		set< Object > geom = new set< Object >;
		set< Object > view = new set< Object >;

		DayZPhysics.RaycastRV( from, to, contact_pos, contact_dir, contact_component, geom, with, ignore, false, false, ObjIntersectGeom, radius );
		DayZPhysics.RaycastRV( from, to, contact_pos, contact_dir, contact_component, view, with, ignore, false, false, ObjIntersectView, radius );
		
		
		if ( geom.Count() ) 
		{
			return geom;		
		}
		if ( geom.Count() ) 
		{
			return view;			
		}
		return NULL;
	}
	
	
    vector GetNavmesh( vector Position, float Radius = 0.5 ) 
    { 
        vector Navmesh;
		

		PGFilter m_pgFilterNav = new PGFilter();
		m_pgFilterNav.SetFlags(PGPolyFlags.WALK, 0, 0);
		
		IsNavmesh = world.SampleNavmeshPosition( Position, Radius, m_pgFilterNav, Navmesh );
		
 		if (IsNavmesh)
		{
			vector SyncNavmesh;
			SyncNavmesh[0] = Navmesh[0];
			SyncNavmesh[1] = m_Soldier.GetPosition()[1];
			SyncNavmesh[2] = Navmesh[2];
			
			vector SyncPosition;
			SyncPosition[0] = Position[0];
			SyncPosition[1] = m_Soldier.GetPosition()[1];
			SyncPosition[2] = Position[2];
			
			
			if (SyncPosition == SyncNavmesh)
			{
				m_IsCollision = false;
			}
			else
			{
				m_IsCollision = true;	
			}
			return Navmesh;
		}	
		return Position;
	}
	
	bool GetMove() { return m_OverrideMove; }
	bool GetWalk() { return m_OverrideWalk; }
	
 	void SendLog(string message) 
	{ 
		Print("AI BOT LOG: " + message);
	} 
}