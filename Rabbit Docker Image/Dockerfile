FROM rabbitmq:management

ADD rabbitmq.conf /etc/rabbitmq/
ADD definitions.json /etc/rabbitmq/


ENTRYPOINT [ "rabbitmq-server" ]