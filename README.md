# Pure ECS Burst Job 2D Grid A* Pathfinding

My goal was to create an easy to use high performant example for myself, as well as other people to incorporate into their 2D projects.

[Forum discussion post](https://forum.unity.com/threads/planning-a-2d-grid-pure-ecs-job-burst-pathfinding.724211)

## Why Pure ECS?
The current project I was working on had performance issues, I resolved some by converting my targeting system to pure ECS.
After I made this change I couldn't believe how amazing the performance was, so I wanted to push things to the limit. 
My previous pathfinding system was the cause for +70% of the CPU strain, so I was bottlenecked by having too many Agents or too large a Map.

## Why Not use Navmesh?
Unity has not shown any love to 2D in the form of navigation, and until that changes I would rather use something that I can quickly adjust
at runtime without having to do work-arounds or hacks to make compatible.
There are other resources for doing so here: [NavMesh+](https://unitylist.com/p/hqq/Nav-Mesh-Plus)
