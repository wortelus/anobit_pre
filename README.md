# AnoBIT Wallet
This is the official wallet software made in .NET WPF. It connects to a remote host to communicate and download/push transactions to its database. It is also a powerful wallet for storing digital funds.


## Network Design
AnoBIT Network is centralized, however the whole chain of transaction is stored and can be verified by the client. Algorithms were taken from Bitcoin's design like ECDSA secp256k1, RIPEMD160(SHA256(PB)) address generation, Base58 etc. The network is created on NANO's  (RaiBlocks) original architecture (send/receive blocks) and has been slightly modified in its core to make it more suitable for AnoBIT's nature. AnoBIT uses send/receive/print transactions.

## Wallet
The AnoBIT Wallet is also a very powerful wallet manager providing basic features like wallet 256bit AES encryption, seed recovery and backup method, manual address creation

## Trust
AnoBIT maintains trust by using the blockchain technology thus even the server owner can not modify existing transactions. However, accounts can be frozen and/or reversed. It is indeed vital to chose a trusted server. The funds across the servers are not typically shared. The server owner and/or the owner of the master public key is able to print funds and send them to addresses.

## Software Management
The AnoBIT's (for both server and client) files are stored in the AppData/Roaming/**AnoBIT** folder including the config, server message (server only), wallets etc.

## Use of the Software
Note that for the successful connection to the server, you have to specify those vars in the config file:
 - remote IP and port to connect
 - replay-attack protection (server must provide its own) to make double-spending across multiple servers harder
 - master public key (server must provide its own) to sign print transactions

## Future
I am planning to work on numerous additional features. The updated list:
- date/time stored in transactions (network upgrade needed)
- mining (in some form) to provide funds creation not dependent on the master printing

## License
The project and all its parts developed by the AnoBIT Software including this program is licensed under **MIT License**
Developed within **AnoBIT Software** and its main core developer, **wortelus**.
