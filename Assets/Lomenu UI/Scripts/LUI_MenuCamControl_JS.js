#pragma strict

public var currentMount : Transform;
public var speed : float = 0.1;
public var zoom = 1.0;
public var cameraComponent : Camera;

private var lastPosition : Vector3;

function Start () {
	lastPosition = transform.position;
}

function Update () {
transform.position = Vector3.Lerp(transform.position,currentMount.position,0.1);
transform.rotation = Quaternion.Slerp(transform.rotation,currentMount.rotation,speed);

var velocity = Vector3.Magnitude(transform.position - lastPosition);
cameraComponent.fieldOfView = 60 + velocity + zoom;

lastPosition = transform.position;
}

function setMount(newMount : Transform) {
	currentMount = newMount;
}