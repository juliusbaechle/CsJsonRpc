using Server;

var reception = new Reception();
var handler = new JobHandler(reception);
var server = new ReceptionServer(reception);

while (true);