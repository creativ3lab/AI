class SurvivorBotDoIdle
{
    protected PlayerBase m_Player;
    protected SurvivorBotBase m_Soldier;

    protected Weapon_Base m_Weapon;

    protected vector m_IdlePosition;
	protected vector m_IdleDirection;
	protected ref Timer m_OnRandomTimer;
	protected bool OnWaypoint;
	ref TVectorArray waypoints = new TVectorArray;
	protected bool IsWaypoints = false;
	protected bool IsUseWaypoints = false;
	protected int m_waypointCount = 0;
	protected int m_WaypointNum = 0;
	
	AIWorld world = GetGame().GetWorld().GetAIWorld();
	ref PGFilter m_pgFilter = new PGFilter();
	

    void SurvivorBotDoIdle(PlayerBase m_Owner, SurvivorBotBase m_SoldierOwner) { m_Player = m_Owner; m_Soldier = m_SoldierOwner; OnEntry(); }
    void ~SurvivorBotDoIdle() { OnExit(); }

    void OnEntry()
    {
		OnWaypoint = true;
		
		m_pgFilter.SetCost(PGAreaType.ROADWAY, 0);
		m_pgFilter.SetFlags(PGPolyFlags.WALK, PGPolyFlags.CROUCH, 0);
		if (!m_OnRandomTimer) 
		{
			m_OnRandomTimer = new Timer(CALL_CATEGORY_GAMEPLAY);
		}
		m_OnRandomTimer.Run( m_Soldier.GetTime() / 1000, this, "OnRandom", null, true );
    }

    void OnRandom()
    {	
        if (m_Soldier && m_Soldier.IsIdle())
        {
            float l_x = m_Soldier.GetPosition()[0] + Math.RandomFloat(m_Soldier.GetRandom() * -1, m_Soldier.GetRandom());
            float l_z = m_Soldier.GetPosition()[2] + Math.RandomFloat(m_Soldier.GetRandom() * -1, m_Soldier.GetRandom());
        //    m_IdlePosition = Vector(l_x, GetGame().SurfaceY(l_x, l_z), l_z);
			m_IdlePosition = GetNewPoint(Vector(l_x, GetGame().SurfaceY(l_x, l_z), l_z));
        }
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
				return pos;
			}
		}
		generate_WayPoint( pos );
		
		return waypoints[m_WaypointNum];
	}
	
	void OnUpdate (float dt) 
    {
        if (m_Soldier && m_Soldier.IsIdle() && m_Soldier.IsBot())
        {
            if(vector.Distance(m_Soldier.GetPosition(), m_IdlePosition) > 1)
		    {
				if (CalculateNewDirection())
				{
                	m_Soldier.GetInputController().OverrideMovementSpeed( true, 1 ); 
                	m_Soldier.GetInputController().OverrideMovementAngle( true, 0 ); 
				}
                m_Soldier.SetDirection(m_IdleDirection);  
		    }
            else
            {
                m_Soldier.GetInputController().OverrideMovementSpeed( false, 0 ); 
           		m_Soldier.GetInputController().OverrideMovementAngle( false, 0 ); 
            }
        }
    }
	
	void OnExit()
	{
		Print("[esbfsm] OnExit " + this);
		if (m_OnRandomTimer)
		{
			m_OnRandomTimer.Stop();
			delete m_OnRandomTimer;
		}
	}	
		
    bool CalculateNewDirection()
	{
		if (m_IdlePosition)
		{
			vector direction_vector = vector.Direction(m_IdlePosition, m_Soldier.GetPosition()).Normalized() * -1;
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

			if (Math.AbsFloat(deltaDir) > 5)
			{	
				float multi = 5;
				if(deltaDir < 0)
				{
					multi = -1;
				}

				bot_angle += (1 * multi);

				float dX = Math.Sin(bot_angle * Math.DEG2RAD);
				float dY = Math.Cos(bot_angle * Math.DEG2RAD);

				new_bot_dir = Vector( dX, 0, dY).Normalized();	
			}
			else
			{
				return true;
			}	

			m_IdleDirection = new_bot_dir;
		}
		
		return false;
	}

	vector CalculateRandomPosition()
	{
		float l_x = m_Soldier.GetPosition()[0] + Math.RandomFloat(20 * -1, 20);
        float l_z = m_Soldier.GetPosition()[2] + Math.RandomFloat(20 * -1, 20);
        return GetNewPoint(Vector(l_x, GetGame().SurfaceY(l_x, l_z), l_z));
	}
}