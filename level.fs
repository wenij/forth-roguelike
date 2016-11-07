require rooms.fs
require tstack.fs

: point@ 
    create c, c, 
    does> dup c@ swap 1+ c@ swap ;

: point
    create c, c,
    does> dup 1+ ;

0 constant NOWAY

create (con-edges) 20 allot
does> swap + ;
variable #edge 

: init-tree
    10 1 do
        i room-cut! loop ;

: init-edges
    #edge off
    20 0 do 0 i (con-edges) ! loop ;

: edge+! 
    4 lshift or #edge dup rot rot @ (con-edges) c! 1 swap +! ;

: edge@
    (con-edges) c@ dup 15 and swap 4 rshift ;

: edge-!
    #edge dup @ if -1 swap +! then ;

: dump-sets
    cr
    10 1 do
        i room-trunk? if [CHAR] T emit 
        else i room-thru? if [CHAR] + emit
        else i room-exists? if [CHAR] 0 emit
        else [CHAR] _ emit then then then
        1 spaces
        i 1- 3 mod 2 = if cr then
    loop 

    #edge @ if
    #edge @ 0 do
        i edge@ '(' emit . . ')' emit
    loop cr then ;

\ return true if the room is nonexistent or has been visited before
: cannot-go? ( i -- b )
    dup room-exists? invert swap room-trunk? or ;

\ Find where we can go starting from given room number.
\ Possible directions are defined as a table of hex numbers.
\ The number of results is variable 0..4, they are room numbers 1..9.
( rn -- directions )
hex
: make-could-go
    create hex 0 , 24 , 135 , 26 , 157 , 2468 , 359 , 48 , 579 , 68 , decimal
    does> swap cells + @ 
        begin
            dup f and dup cannot-go? if drop else swap then
            4 rshift dup 0= until drop ;            
decimal

make-could-go where?

\ pick start room and make it trunk
: pick-start ( -- i ) 
    0 ( put a dummy to drop )
    begin drop 9 rnd1+ 
        dup room-exists? over room-thru? invert and
    until ;

\ pick a random room to go to
: go-to ( from -- to )
    depth >r where? depth r@ - 1+ 
        dup 0> if rnd pick else NOWAY then
        depth r> - 0 do swap drop loop ;

\ prune the edge that connects a dead-end thru room
: prune-last ( rn -- )
    #edge @ if 
        dup                     ( rn rn -- )
        #edge @ 1- edge@        ( rn rn a b -- )
        drop = if 
            -1 #edge +!
            room-nexisteplus!
        else
            drop
        then then ;

\ pick a random room and walk the maze, create edges as we go
: build-tree
    pick-start dup room-trunk!
    begin
        ( rn -- )
        dup go-to dup if 
            ( cur next -- )  
            dup rot dup 
            >tstack edge+!          \ push & keep exploring ( next -- )
            dup room-trunk!
        else
            drop dup                ( cur next -- cur cur )
            room-thru? if dup prune-last then drop
            \ back track to where we can branch or doneski
            tsavail? if tstack> else exit then
        then
    again ;

\ return true if the rooms are all connected
: tree-complete?
    10 1 do
        i room-exists? if
            i room-trunk? invert if
                R> R> drop drop ( unloop )
                false exit
            then
        then
    loop
    true ;

: render-passages
    #edge @ if
        #edge @ 0 do
            i edge@ connect-2rooms
        loop then ;

: add-some-thru
    10 1 do i room-exists? invert if 
        coinflip if i room-thru then 
    then loop ;


: linked-rooms
    C-NOTHING dclear
    make-rooms
    begin 
        init-tree init-edges
        build-tree tree-complete? if exit then   \ all connected, done
        add-some-thru
    again ;

: level
    linked-rooms render-passages render-rooms ;
