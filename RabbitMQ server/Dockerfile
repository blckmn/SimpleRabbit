FROM rabbitmq:management

ADD rabbitmq.conf /etc/rabbitmq/
ADD definitions.json /etc/rabbitmq/

# This is purely for self-documentation - still need to expose the ports in docker run
EXPOSE 5672 15672

ENTRYPOINT [ "rabbitmq-server" ]
