////IMPLEMENTATION OF A* ALGORITHM IN CG/HLSL FOR A 3D INTEGER GRID

struct Node
{
	int3 position;
	Node previous;
}

Node ConstructNode(int3 position)
{
	Node node;
	node.position = position;
	node.previous = ????
	
	return node;
}

Node ConstructNode(int3 position, Node previous)
{
	Node node;
	node.position = position;
	node.previous = previous;
}

Node AStar(Node start, Node end, float h)
{
	Node node;
	return node;
}
