(function () {
    const boardEl = document.getElementById("memoryBoard");
    const movesEl = document.getElementById("movesCounter");
    const resetBtn = document.getElementById("resetMemoryBtn");

    const icons = ["🍎", "🍌", "🍇", "🍓", "🍉", "🍍", "🥝", "🍒"];
    let cards = [];
    let flipped = [];
    let matched = [];
    let moves = 0;
    let lockBoard = false;

    function shuffle(array) {
        return array
            .map(v => ({ v, sort: Math.random() }))
            .sort((a, b) => a.sort - b.sort)
            .map(({ v }) => v);
    }

    function buildBoard() {
        cards = shuffle([...icons, ...icons]);
        flipped = [];
        matched = [];
        moves = 0;
        lockBoard = false;
        movesEl.textContent = "عدد المحاولات: 0";
        render();
    }

    function render() {
        boardEl.innerHTML = "";
        cards.forEach((icon, index) => {
            const card = document.createElement("div");
            const isOpen = flipped.includes(index) || matched.includes(index);
            card.className = "memory-card" + (isOpen ? " flipped" : "");
            card.textContent = isOpen ? icon : "❓";
            card.addEventListener("click", () => handleClick(index));
            boardEl.appendChild(card);
        });
    }

    function handleClick(index) {
        if (lockBoard || flipped.includes(index) || matched.includes(index)) return;

        flipped.push(index);
        render();

        if (flipped.length === 2) {
            moves++;
            movesEl.textContent = `عدد المحاولات: ${moves}`;
            lockBoard = true;

            const [first, second] = flipped;
            if (cards[first] === cards[second]) {
                matched.push(first, second);
                flipped = [];
                lockBoard = false;
                render();

                if (matched.length === cards.length) {
                    setTimeout(() => movesEl.textContent = `🎉 خلصت اللعبة في ${moves} محاولة!`, 200);
                }
            } else {
                setTimeout(() => {
                    flipped = [];
                    lockBoard = false;
                    render();
                }, 700);
            }
        }
    }

    resetBtn.addEventListener("click", buildBoard);
    buildBoard();
})();
