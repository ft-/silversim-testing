default
{
	state_entry()
	{
		key npc = osNpcCreate("Hello", "World", <1,1,1>, "appearance");
		key npc2 = osNpcCreate("Hello", "World", <1,1,1>, "appearance", OS_NPC_NOT_OWNED);
		vector v;
		list l;
		integer i;
		string s;
		rotation r;
		key k;
		v = osNpcGetPos(npc);
		r = osNpcGetRot(npc);
		k = osNpcGetOwner(npc);
		osNpcLoadAppearance(npc, "appearance");
		osNpcMoveTo(npc, <1,1,1>);
		osNpcMoveToTarget(npc, <1,1,1>, OS_NPC_NO_FLY);
		osNpcSaveAppeaance(npc, "appearance2");
		osNpcSay(npc, "Hello");
		osNpcSay(npc, 10, "Hello");
		osNpcSetRot(npc, ZERO_ROTATION);
		osNpcShout(npc, "Hello");
		osNpcShout(npc, 10, "Hello");
		osNpcSit(npc, NULL_KEY, OS_NPC_SIT_NOW);
		osNpcStand(npc);
		osNpcStopMoveToTarget(npc);
		i = osIsNpc(npc);
		osNpcPlayAnimation(npc, "anim");
		osNpcStopAnimation(npc, "anim");
		osNpcTouch(npc, NULL_KEY, 1);
		osNpcWhisper(npc, "Hello");
		osNpcWhisper(npc, 10, "Hello");
		osNpcRemove(npc);
		osNpcRemove(npc2);
	}
}