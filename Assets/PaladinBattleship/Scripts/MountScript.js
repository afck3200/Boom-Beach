private var deployed : boolean;
private var ready : boolean;
var anim : Animation;

function Start()
{
deployed = false;	
}

function Update()
{
	if (ShipStatus.deployed)
	{
		PlayAnimOpen();
	}
	else if (!ShipStatus.deployed)
	{
		PlayAnimClose();
	}
}


function PlayAnimOpen()
{
if (!deployed)
	{
		if (!anim.GetComponent.<Animation>().isPlaying)
		{
			anim.GetComponent.<Animation>().Play("BayDoorsOpen");
			deployed = true;
		}
		else 
		{
			yield; while ( anim.GetComponent.<Animation>().isPlaying ) yield;
			anim.GetComponent.<Animation>().Play("BayDoorsOpen");
		}
		yield; while ( anim.GetComponent.<Animation>().isPlaying ) yield;
		ready = true;
	}
} 

function PlayAnimClose()
{
		if (ready)
	{
		if (!anim.GetComponent.<Animation>().isPlaying)
		{
			ready = false;
			anim.GetComponent.<Animation>().Play("BayDoorsClose");			
		}
		else 
		{
			yield; while ( anim.GetComponent.<Animation>().isPlaying ) yield;
			anim.GetComponent.<Animation>().Play("BayDoorsClose");

		}
		yield; while ( anim.GetComponent.<Animation>().isPlaying ) yield;
		deployed = false;
	}
	
}