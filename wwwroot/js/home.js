function CountUsers(nUsers) {
    const counter = document.getElementById("users-counter__count");
    let countUser = 0;
    const duration = 5000;
    const increment = nUsers / (duration / 50);

    const interval = setInterval(() => {
        countUser += increment;
        if (countUser >= nUsers) {
            countUser = nUsers;
            clearInterval(interval);
        }
        counter.textContent = Math.floor(countUser).toString().padStart(6, "0");
    }, 10);
}