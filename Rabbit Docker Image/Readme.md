# Simple Rabbit Image

This is a minimal instance that will run a Linux dockerized instance of RabbitMq with configuration set up for the example projects to run

## Requirements

[Docker Engine](https://docs.docker.com/), Get the engine based on your Operating system

## Installation

Open Command line within this directory and run the following commands

`docker build -t myrabbitmq .`

`docker run -d --name local-rabbit -p 15672:15672 -p 5672:5672 myrabbitmq`

myrabbitmq can be replaced with any name. the -d flag can be removed if you'd like the commandline to be attached to the container and recieve console log information.

the --name atribute is optional, however will be helpful in restarting the rabbit instance

## Shutdown

To stop the rabbit service, run the following
`docker stop local-rabbit`
`docker rm local-rabbit`

if the --name attribute was not used, `docker ps` can be used to locate the name of the process and container.
