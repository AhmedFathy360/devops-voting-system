const express = require('express');
const { Pool } = require('pg');
const socketIo = require('socket.io');
const http = require('http');

const app = express();
const server = http.createServer(app);
const io = socketIo(server);

const pool = new Pool({
    host: 'postgres',
    user: 'uservote',
    password: 'userpass',
    database: 'votedb',
    port: 5432
});

app.get('/', (req, res) => {
    res.sendFile(__dirname + '/index.html');
});

app.get('/results', async (req, res) => {
    try {
        const result = await pool.query(`
            SELECT vote, COUNT(*) as count 
            FROM votes
            GROUP BY vote
        `);
        res.json(result.rows);
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

io.on('connection', (socket) => {
    console.log('Client connected');
    
    const emitResults = async () => {
        try {
            const result = await pool.query(`
                SELECT vote, COUNT(*) as count 
                FROM votes 
                GROUP BY vote
            `);
            socket.emit('results', result.rows);
        } catch (err) {
            console.error(err);
        }
    };
    
    emitResults();
    const interval = setInterval(emitResults, 2000);
    
    socket.on('disconnect', () => {
        console.log('Client disconnected');
        clearInterval(interval);
    });
});

server.listen(3000, () => {
    console.log('Results app listening on port 3000');
});
